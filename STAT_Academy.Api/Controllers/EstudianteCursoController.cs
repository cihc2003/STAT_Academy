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
        public async Task<IActionResult> ObtenerCursos(int estudianteId)
        {
            if (!PuedeActuarSobre(estudianteId))
                return Forbid();

            return Ok(await _service.ObtenerCursosAsync(estudianteId));
        }

        [Authorize(Roles = Roles.Cliente)]
        [HttpPost("matricular/{cursoId}")]
        public async Task<IActionResult> Matricular(int cursoId)
        {
            var resultado = await _service.MatricularAsync(UsuarioActualId, cursoId);

            if (!resultado)
            {
                return BadRequest(new
                {
                    mensaje =
                        "No se pudo realizar la matrícula. " +
                        "El curso no existe, está inactivo, ya inició, " +
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
