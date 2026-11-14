using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Helpers;
using STAT_Academy.Web.Models.Blog;
using STAT_Academy.Web.Services;
using System.Security.Claims;

namespace STAT_Academy.Web.Controllers
{
    // Los permisos reales los aplica la API (solo el autor edita; inactivas solo para Admin).
    // Aquí se repiten para no mostrar pantallas que después fallarían.
    public class BlogController : Controller
    {
        private readonly ApiBlogService _blogService;

        public BlogController(ApiBlogService blogService)
        {
            _blogService = blogService;
        }

        private int? UsuarioId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        /// <param name="buscar">Texto a buscar en el título o el resumen (sin distinguir mayúsculas ni tildes).</param>
        public async Task<IActionResult> Index(string? buscar)
        {
            var blogs = await _blogService.ObtenerBlogs();

            ViewData["Buscar"] = buscar?.Trim();
            ViewData["Total"] = blogs.Count;

            return View(blogs
                .Where(b => Busqueda.Coincide(b.Titulo, buscar) || Busqueda.Coincide(b.Resumen, buscar))
                .OrderByDescending(b => b.FechaCreacion)
                .ToList());
        }

        public async Task<IActionResult> Details(int id)
        {
            var blog = await _blogService.ObtenerBlogPorId(id);
            if (blog == null)
                return NotFound();

            // "Sigue leyendo": las 3 entradas más recientes además de esta.
            ViewBag.Relacionadas = (await _blogService.ObtenerBlogs())
                .Where(b => b.Id != id && b.Estado)
                .OrderByDescending(b => b.FechaCreacion)
                .Take(3)
                .ToList();

            return View(blog);
        }

        /// <summary>
        /// Descarga un documento adjunto. Los documentos están en un contenedor privado de
        /// Blob Storage: la Web lo pide a la API con el token del usuario y lo reenvía.
        /// </summary>
        public async Task<IActionResult> Documento(int id, int archivoId, CancellationToken ct)
        {
            var respuesta = await _blogService.DescargarDocumento(id, archivoId, ct);
            if (respuesta == null)
                return NotFound();

            HttpContext.Response.RegisterForDispose(respuesta);

            var tipo = respuesta.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var nombre = respuesta.Content.Headers.ContentDisposition?.FileNameStar
                         ?? respuesta.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                         ?? "documento";

            return File(await respuesta.Content.ReadAsStreamAsync(ct), tipo, nombre);
        }

        // ------------------------------------------------------------------
        // Crear
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateBlogViewModel());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(BlogArchivoLimites.MaxBytesPeticion)]
        [RequestFormLimits(MultipartBodyLengthLimit = BlogArchivoLimites.MaxBytesPeticion)]
        public async Task<IActionResult> Create(CreateBlogViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var (exitoso, mensaje, id) = await _blogService.CrearBlog(model);

            if (!exitoso || id == null)
            {
                ModelState.AddModelError(string.Empty, mensaje);
                return View(model);
            }

            if (model.Archivos.Count > 0)
            {
                var subida = await _blogService.SubirArchivos(id.Value, model.Archivos);

                if (!subida.exitoso)
                {
                    // La entrada ya existe: se lleva al autor a editarla para reintentar.
                    TempData["Error"] = "La entrada se publicó, pero los archivos no se guardaron: " + subida.mensaje;
                    return RedirectToAction(nameof(Edit), new { id });
                }
            }

            TempData["Success"] = mensaje;
            return RedirectToAction(nameof(Details), new { id });
        }

        // ------------------------------------------------------------------
        // Editar (solo el autor original)
        // ------------------------------------------------------------------

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var blog = await _blogService.ObtenerBlogPorId(id);
            if (blog == null)
                return NotFound();

            if (blog.FkAutor != UsuarioId)
                return Forbid();

            return View(new UpdateBlogViewModel
            {
                Id = blog.Id,
                Titulo = blog.Titulo,
                Contenido = blog.Contenido,
                Estado = blog.Estado,
                ArchivosActuales = blog.Archivos
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateBlogViewModel model)
        {
            if (id != model.Id)
                return BadRequest();

            var blog = await _blogService.ObtenerBlogPorId(id);
            if (blog == null)
                return NotFound();

            if (blog.FkAutor != UsuarioId)
                return Forbid();

            model.ArchivosActuales = blog.Archivos;

            if (!ModelState.IsValid)
                return View(model);

            var (exitoso, mensaje) = await _blogService.ActualizarBlog(id, model);

            if (!exitoso)
            {
                ModelState.AddModelError(string.Empty, mensaje);
                return View(model);
            }

            TempData["Success"] = "Entrada actualizada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(BlogArchivoLimites.MaxBytesPeticion)]
        [RequestFormLimits(MultipartBodyLengthLimit = BlogArchivoLimites.MaxBytesPeticion)]
        public async Task<IActionResult> SubirArchivos(int id, List<IFormFile> archivos)
        {
            if (archivos.Count == 0)
            {
                TempData["Error"] = "Seleccione al menos un archivo.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var (exitoso, mensaje) = await _blogService.SubirArchivos(id, archivos);
            TempData[exitoso ? "Success" : "Error"] = mensaje;

            return RedirectToAction(nameof(Edit), new { id });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitarArchivo(int id, int archivoId)
        {
            var (exitoso, mensaje) = await _blogService.QuitarArchivo(id, archivoId);
            TempData[exitoso ? "Success" : "Error"] = mensaje;

            return RedirectToAction(nameof(Edit), new { id });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarPrincipal(int id, int archivoId)
        {
            var (exitoso, mensaje) = await _blogService.MarcarPrincipal(id, archivoId);
            TempData[exitoso ? "Success" : "Error"] = mensaje;

            return RedirectToAction(nameof(Edit), new { id });
        }

        /// <summary>
        /// Recibe una imagen del editor (fetch con el token antifalsificación en la cabecera)
        /// y devuelve { url } para insertarla donde está el cursor.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
        public async Task<IActionResult> SubirImagenContenido(IFormFile? imagen)
        {
            if (imagen == null || imagen.Length == 0)
                return BadRequest(new { message = "Seleccione una imagen." });

            var (url, mensaje) = await _blogService.SubirImagenContenido(imagen);
            return url == null ? BadRequest(new { message = mensaje }) : Json(new { url });
        }

        // ------------------------------------------------------------------
        // Publicar / despublicar (autor o Admin)
        // ------------------------------------------------------------------

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            var (exitoso, mensaje) = await _blogService.DesactivarBlog(id);
            TempData[exitoso ? "Success" : "Error"] = mensaje;

            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(int id)
        {
            var (exitoso, mensaje) = await _blogService.ActivarBlog(id);
            TempData[exitoso ? "Success" : "Error"] = mensaje;

            return RedirectToAction(nameof(Index));
        }
    }
}
