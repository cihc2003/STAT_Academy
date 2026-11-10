using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;
using System.Security.Claims;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EstudianteCursoController : ControllerBase
    {
        private readonly EstudianteCursoService _service;

        public EstudianteCursoController(EstudianteCursoService service)
        {
            _service = service;
        }

        [HttpGet("{estudianteId}")]
        public IActionResult ObtenerCursos(int estudianteId)
        {
            return Ok(_service.ObtenerCursos(estudianteId));
        }
        [Authorize(Roles = "ESTUDIANTE")]
        [HttpPost("matricular/{cursoId}")]
        public IActionResult Matricular(int cursoId)
        {
            var idUsuarioClaim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (idUsuarioClaim == null)
            {
                return Unauthorized(new
                {
                    mensaje = "No se pudo identificar al usuario."
                });
            }

            int estudianteId =
                int.Parse(idUsuarioClaim.Value);

            var resultado =
                _service.Matricular(
                    estudianteId,
                    cursoId
                );

            if (!resultado)
            {
                return BadRequest(new
                {
                    mensaje =
                        "No se pudo realizar la matrícula. " +
                        "El curso no existe, está inactivo " +
                        "o ya está matriculado."
                });
            }

            return Ok(new
            {
                mensaje = "Curso matriculado correctamente."
            });
        }
    }
}