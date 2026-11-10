using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Curso;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    [Route("api/[controller]")]
    public class CursoController : ControllerBase
    {
        private readonly CursoService _service;

        public CursoController(CursoService service)
        {
            _service = service;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_service.GetAll());
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var curso = _service.GetById(id);

            if (curso == null)
                return NotFound();

            return Ok(curso);
        }

        // La duración del curso se calcula con las fechas, así que deben ser coherentes.
        private const string FechasInvalidas =
            "La fecha de fin no puede ser anterior a la fecha de inicio.";

        [HttpPost]
        public IActionResult Crear(CursoCreateRequest request)
        {
            if (request.fechaFin < request.fechaInicio)
                return BadRequest(new { message = FechasInvalidas });

            return Ok(_service.Crear(request));
        }

        [HttpPut("{id}")]
        public IActionResult Editar(int id, CursoCreateRequest request)
        {
            if (request.fechaFin < request.fechaInicio)
                return BadRequest(new { message = FechasInvalidas });

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