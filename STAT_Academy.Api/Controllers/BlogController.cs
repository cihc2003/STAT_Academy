using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Blogs;

namespace STAT_Academy.Api.Controllers
{
    /// <summary>
    /// Reglas del blog:
    ///  - Cualquiera ve las entradas ACTIVAS; solo Admin ve también las inactivas.
    ///  - Solo Admin crea entradas.
    ///  - Solo el AUTOR original edita la entrada y gestiona sus archivos.
    ///  - Activar/desactivar: el autor o un Admin (moderación).
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    public class BlogController : ApiControllerBase
    {
        private readonly BlogService _service;

        public BlogController(BlogService service)
        {
            _service = service;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _service.GetBlogsAsync(incluirInactivos: EsAdmin));
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var blog = await _service.GetBlogByIdAsync(id, incluirInactivos: EsAdmin);

            if (blog == null)
                return NotFound(new { message = "La entrada no existe o no está disponible." });

            return Ok(blog);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create(CreateBlogRequest request)
        {
            var (blog, error) = await _service.CrearBlogAsync(request, UsuarioActualId, UsuarioActualEmail);

            if (blog == null)
                return BadRequest(new { message = error });

            return CreatedAtAction(nameof(GetById), new { id = blog.id }, blog);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateBlogRequest request)
        {
            var permiso = await SoloAutorAsync(id);
            if (permiso != null)
                return permiso;

            var (blog, error) = await _service.ActualizarBlogAsync(id, request, UsuarioActualEmail);

            if (error != null)
                return BadRequest(new { message = error });

            return blog == null ? NotFound(new { message = "La entrada no existe." }) : Ok(blog);
        }

        /// <summary>
        /// Sube una imagen para insertarla en el texto desde el editor. Devuelve { url }.
        /// Solo Admin (los autores del blog son administradores).
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("imagenes-contenido")]
        [RequestSizeLimit(BlogArchivoValidador.MaxBytesImagen + 512 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = BlogArchivoValidador.MaxBytesImagen + 512 * 1024)]
        public async Task<IActionResult> SubirImagenContenido([FromForm] IFormFile? imagen, CancellationToken ct)
        {
            var (url, error) = await _service.SubirImagenContenido(imagen, ct);
            return url == null ? BadRequest(new { message = error }) : Ok(new { url });
        }

        [HttpPatch("{id}/desactivar")]
        public async Task<IActionResult> Desactivar(int id)
        {
            var permiso = await AutorOAdminAsync(id);
            if (permiso != null)
                return permiso;

            await _service.DesactivarBlogAsync(id, UsuarioActualEmail);

            return Ok(new { message = "La entrada fue desactivada y ya no es visible para los clientes." });
        }

        [HttpPatch("{id}/activar")]
        public async Task<IActionResult> Activar(int id)
        {
            var permiso = await AutorOAdminAsync(id);
            if (permiso != null)
                return permiso;

            var actual = (await _service.GetBlogByIdAsync(id, incluirInactivos: true))!;

            await _service.ActualizarBlogAsync(id, new UpdateBlogRequest
            {
                titulo = actual.titulo,
                contenido = actual.contenido,
                estado = true
            }, UsuarioActualEmail);

            return Ok(new { message = "La entrada fue publicada nuevamente." });
        }

        // ---------------------------------------------------------------
        // Descarga de documentos (contenedor privado): mismas reglas que ver la entrada
        // ---------------------------------------------------------------

        [AllowAnonymous]
        [HttpGet("{id}/archivos/{archivoId}/descargar")]
        public async Task<IActionResult> DescargarDocumento(int id, int archivoId, CancellationToken ct)
        {
            var documento = await _service.AbrirDocumento(id, archivoId, incluirInactivos: EsAdmin, ct);

            if (documento == null)
                return NotFound(new { message = "El documento no existe o no está disponible." });

            var (contenido, nombre) = documento.Value;

            return File(contenido.Contenido, contenido.ContentType ?? "application/octet-stream", nombre);
        }

        // ---------------------------------------------------------------
        // Archivos: solo el autor
        // ---------------------------------------------------------------

        [HttpPost("{id}/archivos")]
        [RequestSizeLimit(BlogArchivoValidador.MaxBytesPeticion)]
        [RequestFormLimits(MultipartBodyLengthLimit = BlogArchivoValidador.MaxBytesPeticion)]
        public async Task<IActionResult> SubirArchivos(int id, [FromForm] List<IFormFile> archivos)
        {
            var permiso = await SoloAutorAsync(id);
            if (permiso != null)
                return permiso;

            var (blog, errores) = await _service.AgregarArchivos(id, archivos, UsuarioActualEmail);

            if (blog == null)
            {
                return BadRequest(new
                {
                    message = string.Join(" ", errores),
                    errores
                });
            }

            return Ok(blog);
        }

        [HttpDelete("{id}/archivos/{archivoId}")]
        public async Task<IActionResult> QuitarArchivo(int id, int archivoId)
        {
            var permiso = await SoloAutorAsync(id);
            if (permiso != null)
                return permiso;

            if (!await _service.QuitarArchivo(id, archivoId, UsuarioActualEmail))
                return NotFound(new { message = "El archivo no existe en esta entrada." });

            return Ok(new { message = "Archivo eliminado." });
        }

        [HttpPatch("{id}/archivos/{archivoId}/principal")]
        public async Task<IActionResult> MarcarPrincipal(int id, int archivoId)
        {
            var permiso = await SoloAutorAsync(id);
            if (permiso != null)
                return permiso;

            var (exitoso, mensaje) = await _service.MarcarPrincipalAsync(id, archivoId);

            return exitoso
                ? Ok(new { message = mensaje })
                : BadRequest(new { message = mensaje });
        }

        // ---------------------------------------------------------------

        /// <summary>null si el usuario es el autor; si no, la respuesta de error.</summary>
        private async Task<IActionResult?> SoloAutorAsync(int blogId)
        {
            var autorId = await _service.ObtenerAutorIdAsync(blogId);

            if (autorId == null)
                return NotFound(new { message = "La entrada no existe." });

            if (autorId != UsuarioActualId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Solo el autor original puede modificar esta entrada." });

            return null;
        }

        private async Task<IActionResult?> AutorOAdminAsync(int blogId)
        {
            var autorId = await _service.ObtenerAutorIdAsync(blogId);

            if (autorId == null)
                return NotFound(new { message = "La entrada no existe." });

            if (!PuedeActuarSobre(autorId.Value))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Solo el autor o un administrador pueden cambiar el estado de esta entrada." });

            return null;
        }
    }
}
