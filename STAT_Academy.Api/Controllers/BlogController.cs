using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Blogs;


namespace STAT_Academy.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class BlogController : ControllerBase
    {


        private readonly BlogService _service;


        public BlogController(BlogService service)
        {
            _service = service;
        }



        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_service.GetBlogs());
        }



        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {

            var blog = _service.GetBlogById(id);


            if (blog == null)
                return NotFound();


            return Ok(blog);

        }


        [Authorize(Roles = "ADMIN,TUTOR")]
        [HttpPost]
        public IActionResult Create(
            CreateBlogRequest request)
        {

            return Ok(
                _service.CrearBlog(request)
            );

        }


        [Authorize(Roles = "ADMIN,TUTOR")]
        [HttpPut("{id}")]
        public IActionResult Update(
            int id,
            UpdateBlogRequest request)
        {

            var blog =
                _service.ActualizarBlog(id, request);


            if (blog == null)
                return NotFound();


            return Ok(blog);

        }


        [Authorize(Roles = "ADMIN,TUTOR")]
        [HttpPatch("{id}/desactivar")]
        public IActionResult Delete(int id)
        {

            var blog =
                _service.DesactivarBlog(id);


            if (blog == null)
                return NotFound();


            return Ok(blog);

        }

    }

}