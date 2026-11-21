using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Seguridad;
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
        private readonly LoginService _loginService;
        private readonly TokenService _tokenService;
        private readonly LimitadorPorCorreo _limitador;
        private readonly ILogger<ContrasenaController> _logger;

        public ContrasenaController(
            ContrasenaService contrasenaService,
            CorreoService correoService,
            LoginService loginService,
            TokenService tokenService,
            LimitadorPorCorreo limitador,
            ILogger<ContrasenaController> logger)
        {
            _contrasenaService = contrasenaService;
            _correoService = correoService;
            _loginService = loginService;
            _tokenService = tokenService;
            _limitador = limitador;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("solicitar-recuperacion")]
        [EnableRateLimiting(LimitesSolicitudes.Anonimo)]
        public async Task<IActionResult> SolicitarRecuperacion(
            SolicitarRecuperacionRequest request)
        {
            // Pasado el límite por dirección no se genera ni envía nada, pero la respuesta es la misma
            // (así no se puede usar para saber qué correos existen ni para inundar un buzón).
            var token = _limitador.PermitirRecuperacion(request.email)
                ? await _contrasenaService.GenerarTokenRecuperacionAsync(request.email)
                : null;

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
        [EnableRateLimiting(LimitesSolicitudes.Anonimo)]
        public async Task<IActionResult> RestablecerContrasena(
            RestablecerContrasenaRequest request)
        {
            var resultado = await _contrasenaService
                .RestablecerContrasenaAsync(request);

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
        [EnableRateLimiting(LimitesSolicitudes.CambioContrasena)]
        public async Task<IActionResult> CambiarContrasena(
            CambiarContrasenaRequest request)
        {
            // Solo se puede cambiar la contraseña propia: el id sale del token.
            var resultado = await _contrasenaService
                .CambiarContrasenaAsync(UsuarioActualId, request);

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

            // El cambio revoca los tokens anteriores (ver ValidadorSesion), incluido el de esta
            // sesión: se devuelve uno nuevo para que el cliente siga conectado.
            var sesion = await _loginService.SesionDeAsync(UsuarioActualId);
            if (sesion == null)
                return Ok(new { message = resultado.mensaje });

            var (token, expiraEn) = _tokenService.GenerarToken(sesion.Datos, sesion.Sello);
            return Ok(new { message = resultado.mensaje, token, expiraEn });
        }
    }
}