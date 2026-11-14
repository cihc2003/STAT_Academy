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
        public IActionResult ObtenerPorCurso(int cursoId)
        {
            if (!PuedeVerCurso(cursoId))
                return Forbid();

            return Ok(_service.ObtenerPorCurso(cursoId));
        }

        [HttpGet("{id}/descargar")]
        public async Task<IActionResult> Descargar(int id, CancellationToken ct)
        {
            var cursoId = _tutor.CursoDeMaterial(id);
            if (cursoId == null)
                return NotFound(new { message = "El material no existe." });

            if (!PuedeVerCurso(cursoId.Value))
                return Forbid();

            var archivo = await _tutor.AbrirMaterial(id, ct);
            if (archivo == null)
                return NotFound(new { message = "El material no existe o no está disponible." });

            var (contenido, nombre) = archivo.Value;
            return File(contenido.Contenido, contenido.ContentType ?? "application/octet-stream", nombre);
        }

        private bool PuedeVerCurso(int cursoId)
        {
            if (EsAdmin) return true;
            var (existe, tutorId) = _tutor.TutorDelCurso(cursoId);
            return existe && (EsTutor(tutorId) || _tutor.PuedeVerComoEstudiante(cursoId, UsuarioActualId));
        }
    }
}
