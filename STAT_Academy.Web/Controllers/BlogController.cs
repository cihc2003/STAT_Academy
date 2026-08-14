using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Models;
using STAT_Academy.Web.Services;


namespace STAT_Academy.Web.Controllers
{

    public class BlogController : Controller
    {

        private readonly ApiBlogService _service;


        public BlogController(ApiBlogService service)
        {
            _service = service;
        }



        public async Task<IActionResult> Index()
        {

            var blogs = await _service.GetBlogs();

            var publicados = blogs.Where(b => b.estado).ToList();

            return View(blogs);

        }



        public async Task<IActionResult> Details(int id)
        {

            var blog =
                await _service.GetBlog(id);


            if (blog == null)
                return NotFound();


            return View(blog);

        }



        public IActionResult Create()
        {
            return View();
        }



        [HttpPost]
        public async Task<IActionResult> Create(
            BlogViewModel blog)
        {

            if (ModelState.IsValid)
            {

                await _service.Crear(blog);

                return RedirectToAction(nameof(Index));

            }


            return View(blog);

        }




        public async Task<IActionResult> Edit(int id)
        {

            var blog =
                await _service.GetBlog(id);


            if (blog == null)
                return NotFound();


            return View(blog);

        }



        [HttpPost]
        public async Task<IActionResult> Edit(
            int id,
            BlogViewModel blog)
        {

            await _service.Actualizar(id, blog);


            return RedirectToAction(nameof(Index));

        }



        public async Task<IActionResult> Delete(int id)
        {

            await _service.Eliminar(id);


            return RedirectToAction(nameof(Index));

        }

    }
}