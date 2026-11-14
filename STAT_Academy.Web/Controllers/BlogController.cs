using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Models.Blog;
using STAT_Academy.Web.Services;
using System.Security.Claims;

namespace STAT_Academy.Web.Controllers
{
    public class BlogController : Controller
    {
        private readonly ApiBlogService _blogService;

        public BlogController(ApiBlogService blogService)
        {
            _blogService = blogService;
        }

        private int? UsuarioId
        {
            get
            {
                var idTexto = User.FindFirstValue(ClaimTypes.NameIdentifier);
                return int.TryParse(idTexto, out var id) ? id : null;
            }
        }

        public async Task<IActionResult> Index()
        {
            var blogs = await _blogService.ObtenerBlogs();
            return View(blogs);
        }

        public async Task<IActionResult> Details(int id)
        {
            var blog = await _blogService.ObtenerBlogPorId(id);
            if (blog == null)
            {
                return NotFound();
            }

            return View(blog);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateBlogViewModel());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBlogViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuarioId = UsuarioId;
            if (usuarioId == null)
            {
                return RedirectToAction("Login", "Cuenta");
            }

            model.FkAutor = usuarioId.Value;

            var resultado = await _blogService.CrearBlog(model);

            if (!resultado.exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.mensaje);
                return View(model);
            }

            TempData["Mensaje"] = resultado.mensaje;
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var blog = await _blogService.ObtenerBlogPorId(id);
            if (blog == null)
            {
                return NotFound();
            }

            var usuarioId = UsuarioId;
            if (usuarioId == null)
            {
                return RedirectToAction("Login", "Cuenta");
            }

            if (blog.FkAutor != usuarioId.Value)
            {
                return Forbid();
            }

            return View(new UpdateBlogViewModel
            {
                Id = blog.Id,
                Titulo = blog.Titulo,
                Contenido = blog.Contenido,
                Estado = blog.Estado
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateBlogViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var blog = await _blogService.ObtenerBlogPorId(id);
            if (blog == null)
            {
                return NotFound();
            }

            var usuarioId = UsuarioId;
            if (usuarioId == null)
            {
                return RedirectToAction("Login", "Cuenta");
            }

            if (blog.FkAutor != usuarioId.Value)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var resultado = await _blogService.ActualizarBlog(id, model);

            if (!resultado.exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.mensaje);
                return View(model);
            }

            TempData["Mensaje"] = resultado.mensaje;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            var resultado = await _blogService.DesactivarBlog(id);

            if (!resultado.exitoso)
            {
                TempData["Error"] = resultado.mensaje;
                return RedirectToAction(nameof(Index));
            }

            TempData["Mensaje"] = resultado.mensaje;
            return RedirectToAction(nameof(Index));
        }
    }
}