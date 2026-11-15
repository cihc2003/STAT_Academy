using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Curso;

namespace STAT_Academy.Api.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    [Route("api/[controller]")]
    public class CursoController : ApiControllerBase
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
            // El catálogo público solo muestra cursos activos que no han iniciado; Admin ve todos.
            return Ok(_service.GetAll(esAdmin: EsAdmin));
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var curso = _service.GetById(id);

            // Un curso inactivo solo lo ven Admin y su propio tutor; para el resto "no existe".
            var esSuTutor = curso != null && EsTutor(curso.fk_tutor);

            if (curso == null || (!curso.estado && !EsAdmin && !esSuTutor))
                return NotFound();

            // Un curso ya iniciado sí se puede consultar (inscripcionAbierta = false).
            return Ok(_service.GetResponse(id));
        }

        // La duración del curso se calcula con las fechas, así que deben ser coherentes.
        private const string FechasInvalidas =
            "La fecha de fin no puede ser anterior a la fecha de inicio.";

        [HttpPost]
        public IActionResult Crear(CursoCreateRequest request)
        {
            if (request.fechaFin < request.fechaInicio)
                return BadRequest(new { message = FechasInvalidas });

            if (_service.ValidarTutor(request.fk_tutor) is string errorTutor)
                return BadRequest(new { message = errorTutor });

            return Ok(_service.Crear(request, UsuarioActualId));
        }

        [HttpPut("{id}")]
        public IActionResult Editar(int id, CursoCreateRequest request)
        {
            if (request.fechaFin < request.fechaInicio)
                return BadRequest(new { message = FechasInvalidas });

            if (_service.ValidarTutor(request.fk_tutor) is string errorTutor)
                return BadRequest(new { message = errorTutor });

            var curso = _service.Editar(id, request);

            if (curso == null)
                return NotFound();

            return Ok(curso);
        }

        // ---------------------------------------------------------------
        // Portada (una sola imagen por curso; subir otra la reemplaza)
        // ---------------------------------------------------------------

        [HttpPost("{id:int}/portada")]
        [RequestSizeLimit(BlogArchivoValidador.MaxBytesImagen + 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = BlogArchivoValidador.MaxBytesImagen + 1024 * 1024)]
        public async Task<IActionResult> SubirPortada(int id, [FromForm] IFormFile? imagen, CancellationToken ct)
        {
            var (curso, error) = await _service.CambiarPortada(id, imagen, UsuarioActualEmail, ct);

            if (curso == null)
                return error == "El curso no existe."
                    ? NotFound(new { message = error })
                    : BadRequest(new { message = error });

            return Ok(curso);
        }

        [HttpDelete("{id:int}/portada")]
        public async Task<IActionResult> QuitarPortada(int id) =>
            await _service.QuitarPortada(id, UsuarioActualEmail)
                ? Ok(new { message = "Portada eliminada." })
                : NotFound(new { message = "El curso no tiene portada." });

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