using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Curso;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CursoController : ControllerBase
    {
        private readonly CursoService _service;

        public CursoController(CursoService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_service.GetAll());
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var curso = _service.GetById(id);

            if (curso == null)
                return NotFound();

            return Ok(curso);
        }

        [HttpPost]
        public IActionResult Crear(CursoCreateRequest request)
        {
            return Ok(_service.Crear(request));
        }

        [HttpPut("{id}")]
        public IActionResult Editar(int id, CursoCreateRequest request)
        {
            var curso = _service.Editar(id, request);

            if (curso == null)
                return NotFound();

            return Ok(curso);
        }

        [HttpPatch("activar/{id}")]
        public IActionResult Activar(int id)
        {
            var curso = _service.Activar(id);

            if (curso == null)
                return NotFound();

            return Ok(curso);
        }

        [HttpPatch("desactivar/{id}")]
        public IActionResult Desactivar(int id)
        {
            var curso = _service.Desactivar(id);

            if (curso == null)
                return NotFound();

            return Ok(curso);
        }
    }
}