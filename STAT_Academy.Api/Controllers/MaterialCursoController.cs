using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    /// <summary>Material del curso para el aula: matriculados, tutor del curso y Admin.</summary>
    [Authorize]
    [Route("api/[controller]")]
    public class MaterialCursoController : ApiControllerBase
    {
        private readonly MaterialCursoService _service;
        private readonly TutorService _tutor;

        public MaterialCursoController(MaterialCursoService service, TutorService tutor)
        {
            _service = service;
            _tutor = tutor;
        }

        [HttpGet("curso/{cursoId}")]
        public async Task<IActionResult> ObtenerPorCurso(int cursoId)
        {
            if (!await PuedeVerCurso(cursoId))
                return Forbid();

            return Ok(await _service.ObtenerPorCursoAsync(cursoId));
        }

        [HttpGet("{id}/descargar")]
        public async Task<IActionResult> Descargar(int id, CancellationToken ct)
        {
            var cursoId = await _tutor.CursoDeMaterialAsync(id);
            if (cursoId == null)
                return NotFound(new { message = "El material no existe." });

            if (!await PuedeVerCurso(cursoId.Value))
                return Forbid();

            var archivo = await _tutor.AbrirMaterial(id, ct);
            if (archivo == null)
                return NotFound(new { message = "El material no existe o no está disponible." });

            var (contenido, nombre) = archivo.Value;
            return File(contenido.Contenido, contenido.ContentType ?? "application/octet-stream", nombre);
        }

        private async Task<bool> PuedeVerCurso(int cursoId)
        {
            if (EsAdmin) return true;
            var (existe, tutorId) = await _tutor.TutorDelCursoAsync(cursoId);
            return existe && (EsTutor(tutorId) || await _tutor.PuedeVerComoEstudianteAsync(cursoId, UsuarioActualId));
        }
    }
}
