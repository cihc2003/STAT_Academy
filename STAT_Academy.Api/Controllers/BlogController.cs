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
        public IActionResult Get()
        {
            return Ok(_service.GetBlogs(incluirInactivos: EsAdmin));
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var blog = _service.GetBlogById(id, incluirInactivos: EsAdmin);

            if (blog == null)
                return NotFound(new { message = "La entrada no existe o no está disponible." });

            return Ok(blog);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public IActionResult Create(CreateBlogRequest request)
        {
            var blog = _service.CrearBlog(request, UsuarioActualId, UsuarioActualEmail);

            return CreatedAtAction(nameof(GetById), new { id = blog.id }, blog);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, UpdateBlogRequest request)
        {
            var permiso = SoloAutor(id);
            if (permiso != null)
                return permiso;

            return Ok(_service.ActualizarBlog(id, request, UsuarioActualEmail));
        }

        [HttpPatch("{id}/desactivar")]
        public IActionResult Desactivar(int id)
        {
            var permiso = AutorOAdmin(id);
            if (permiso != null)
                return permiso;

            _service.DesactivarBlog(id, UsuarioActualEmail);

            return Ok(new { message = "La entrada fue desactivada y ya no es visible para los clientes." });
        }

        [HttpPatch("{id}/activar")]
        public IActionResult Activar(int id)
        {
            var permiso = AutorOAdmin(id);
            if (permiso != null)
                return permiso;

            var actual = _service.GetBlogById(id, incluirInactivos: true)!;

            _service.ActualizarBlog(id, new UpdateBlogRequest
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
            var permiso = SoloAutor(id);
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
            var permiso = SoloAutor(id);
            if (permiso != null)
                return permiso;

            if (!await _service.QuitarArchivo(id, archivoId, UsuarioActualEmail))
                return NotFound(new { message = "El archivo no existe en esta entrada." });

            return Ok(new { message = "Archivo eliminado." });
        }

        [HttpPatch("{id}/archivos/{archivoId}/principal")]
        public IActionResult MarcarPrincipal(int id, int archivoId)
        {
            var permiso = SoloAutor(id);
            if (permiso != null)
                return permiso;

            var (exitoso, mensaje) = _service.MarcarPrincipal(id, archivoId);

            return exitoso
                ? Ok(new { message = mensaje })
                : BadRequest(new { message = mensaje });
        }

        // ---------------------------------------------------------------

        /// <summary>null si el usuario es el autor; si no, la respuesta de error.</summary>
        private IActionResult? SoloAutor(int blogId)
        {
            var autorId = _service.ObtenerAutorId(blogId);

            if (autorId == null)
                return NotFound(new { message = "La entrada no existe." });

            if (autorId != UsuarioActualId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Solo el autor original puede modificar esta entrada." });

            return null;
        }

        private IActionResult? AutorOAdmin(int blogId)
        {
            var autorId = _service.ObtenerAutorId(blogId);

            if (autorId == null)
                return NotFound(new { message = "La entrada no existe." });

            if (!PuedeActuarSobre(autorId.Value))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Solo el autor o un administrador pueden cambiar el estado de esta entrada." });

            return null;
        }
    }
}
