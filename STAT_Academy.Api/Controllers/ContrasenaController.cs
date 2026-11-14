using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Contrasena;

namespace STAT_Academy.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class ContrasenaController : ApiControllerBase
    {
        private readonly ContrasenaService _contrasenaService;
        private readonly CorreoService _correoService;
        private readonly ILogger<ContrasenaController> _logger;

        public ContrasenaController(
            ContrasenaService contrasenaService,
            CorreoService correoService,
            ILogger<ContrasenaController> logger)
        {
            _contrasenaService = contrasenaService;
            _correoService = correoService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("solicitar-recuperacion")]
        public async Task<IActionResult> SolicitarRecuperacion(
            SolicitarRecuperacionRequest request)
        {
            var token = _contrasenaService
                .GenerarTokenRecuperacion(request.email);

            if (token != null)
            {
                try
                {
                    await _correoService.EnviarRecuperacionContrasena(
                        request.email,
                        token
                    );
                }
                catch (Exception ex)
                {
                    // Misma respuesta que si el correo no existiera: un 500 solo para
                    // cuentas existentes permitiría averiguar qué correos están registrados.
                    _logger.LogError(ex, "No se pudo enviar el correo de recuperación de contraseña.");
                }
            }

            return Ok(new
            {
                message =
                    "Si el correo está registrado, recibirá un enlace " +
                    "para restablecer la contraseña."
            });
        }

        [AllowAnonymous]
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
        public async Task<IActionResult> CambiarContrasena(
            CambiarContrasenaRequest request)
        {
            // Solo se puede cambiar la contraseña propia: el id sale del token.
            var resultado = _contrasenaService
                .CambiarContrasena(UsuarioActualId, request);

            if (!resultado.exitoso)
            {
                return BadRequest(new { message = resultado.mensaje });
            }

            // Aviso de seguridad al titular; si falla, el cambio ya está hecho.
            if (!string.IsNullOrWhiteSpace(resultado.email))
            {
                try
                {
                    await _correoService.EnviarAvisoCambioContrasena(
                        resultado.email,
                        resultado.nombre ?? resultado.email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "No se pudo enviar el aviso de cambio de contraseña al usuario {UsuarioId}.",
                        UsuarioActualId);
                }
            }

            return Ok(new { message = resultado.mensaje });
        }
    }
}