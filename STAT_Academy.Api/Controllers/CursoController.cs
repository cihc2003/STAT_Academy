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
        public async Task<IActionResult> GetAll()
        {
            // El catálogo público solo muestra cursos activos que no han iniciado; Admin ve todos.
            return Ok(await _service.GetAllAsync(esAdmin: EsAdmin));
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var curso = await _service.GetByIdAsync(id);

            // Un curso inactivo solo lo ven Admin y su propio tutor; para el resto "no existe".
            var esSuTutor = curso != null && EsTutor(curso.fk_tutor);

            if (curso == null || (!curso.estado && !EsAdmin && !esSuTutor))
                return NotFound();

            // Un curso ya iniciado sí se puede consultar (inscripcionAbierta = false).
            return Ok(await _service.GetResponseAsync(id));
        }

        // La duración del curso se calcula con las fechas, así que deben ser coherentes.
        private const string FechasInvalidas =
            "La fecha de fin no puede ser anterior a la fecha de inicio.";

        [HttpPost]
        public async Task<IActionResult> Crear(CursoCreateRequest request)
        {
            if (request.fechaFin < request.fechaInicio)
                return BadRequest(new { message = FechasInvalidas });

            if (await _service.ValidarTutorAsync(request.fk_tutor) is string errorTutor)
                return BadRequest(new { message = errorTutor });

            return Ok(await _service.CrearAsync(request, UsuarioActualId));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(int id, CursoCreateRequest request)
        {
            if (request.fechaFin < request.fechaInicio)
                return BadRequest(new { message = FechasInvalidas });

            if (await _service.ValidarTutorAsync(request.fk_tutor) is string errorTutor)
                return BadRequest(new { message = errorTutor });

            var curso = await _service.EditarAsync(id, request);

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
        public async Task<IActionResult> Activar(int id)
        {
            var curso = await _service.ActivarAsync(id);

            if (curso == null)
                return NotFound();

            return Ok(curso);
        }

        [HttpPatch("desactivar/{id}")]
        public async Task<IActionResult> Desactivar(int id)
        {
            var curso = await _service.DesactivarAsync(id);

            if (curso == null)
                return NotFound();

            return Ok(curso);
        }
    }
}