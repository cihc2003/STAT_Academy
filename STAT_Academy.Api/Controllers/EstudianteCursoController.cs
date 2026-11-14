using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class EstudianteCursoController : ApiControllerBase
    {
        private readonly EstudianteCursoService _service;

        public EstudianteCursoController(EstudianteCursoService service)
        {
            _service = service;
        }

        [HttpGet("{estudianteId}")]
        public IActionResult ObtenerCursos(int estudianteId)
        {
            if (!PuedeActuarSobre(estudianteId))
                return Forbid();

            return Ok(_service.ObtenerCursos(estudianteId));
        }

        [Authorize(Roles = Roles.Cliente)]
        [HttpPost("matricular/{cursoId}")]
        public IActionResult Matricular(int cursoId)
        {
            var resultado = _service.Matricular(UsuarioActualId, cursoId);

            if (!resultado)
            {
                return BadRequest(new
                {
                    mensaje =
                        "No se pudo realizar la matrícula. " +
                        "El curso no existe, está inactivo, " +
                        "es de pago (debe comprarse) " +
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
