using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Contrasena;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContrasenaController : ControllerBase
    {
        private readonly ContrasenaService _contrasenaService;
        private readonly CorreoService _correoService;

        public ContrasenaController(
            ContrasenaService contrasenaService,
            CorreoService correoService)
        {
            _contrasenaService = contrasenaService;
            _correoService = correoService;
        }

        [HttpPost("solicitar-recuperacion")]
        public async Task<IActionResult> SolicitarRecuperacion(
            SolicitarRecuperacionRequest request)
        {
            var token = _contrasenaService
                .GenerarTokenRecuperacion(request.email);

            if (token != null)
            {
                await _correoService.EnviarRecuperacionContrasena(
                    request.email,
                    token
                );
            }

            return Ok(new
            {
                message =
                    "Si el correo está registrado, recibirá un enlace " +
                    "para restablecer la contraseña."
            });
        }

        [HttpPost("restablecer")]
        public IActionResult RestablecerContrasena(
            RestablecerContrasenaRequest request)
        {
            var resultado = _contrasenaService
                .RestablecerContrasena(request);

            if (!resultado)
            {
                return BadRequest(new
                {
                    message =
                        "El enlace es inválido, ya fue utilizado o expiró."
                });
            }

            return Ok(new
            {
                message = "La contraseña fue restablecida correctamente."
            });
        }

        [HttpPost("cambiar")]
        public IActionResult CambiarContrasena(
            CambiarContrasenaRequest request)
        {
            var resultado = _contrasenaService
                .CambiarContrasena(request);

            if (!resultado)
            {
                return BadRequest(new
                {
                    message =
                        "No se pudo cambiar la contraseña. " +
                        "Verifique la contraseña actual."
                });
            }

            return Ok(new
            {
                message = "La contraseña fue cambiada correctamente."
            });
        }
    }
}