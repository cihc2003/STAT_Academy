using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    /// <summary>Tareas del curso para el aula: matriculados, tutor del curso y Admin.</summary>
    [Authorize]
    [Route("api/[controller]")]
    public class TareaController : ApiControllerBase
    {
        private readonly TareaService _service;
        private readonly TutorService _tutor;

        public TareaController(TareaService service, TutorService tutor)
        {
            _service = service;
            _tutor = tutor;
        }

        [HttpGet("curso/{cursoId}")]
        public IActionResult ObtenerCurso(int cursoId)
        {
            if (!EsAdmin)
            {
                var (existe, tutorId) = _tutor.TutorDelCurso(cursoId);
                if (!existe || (!EsTutor(tutorId) && !_tutor.PuedeVerComoEstudiante(cursoId, UsuarioActualId)))
                    return Forbid();
            }

            return Ok(_service.ObtenerPorCurso(cursoId));
        }
    }
}
