using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Seguridad;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class LoginController : ApiControllerBase
    {
        private readonly LoginService _loginService;
        private readonly TokenService _tokenService;
        private readonly LimitadorPorCorreo _limitador;

        public LoginController(LoginService loginService, TokenService tokenService, LimitadorPorCorreo limitador)
        {
            _loginService = loginService;
            _tokenService = tokenService;
            _limitador = limitador;
        }

        [HttpPost]
        [EnableRateLimiting(LimitesSolicitudes.Anonimo)]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            // Por cuenta: frena la fuerza bruta aunque el atacante cambie de IP.
            if (!_limitador.PermitirLogin(model.email))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { message = LimitesSolicitudes.MensajeRechazo });
            }

            var (resultado, sesion) = await _loginService.LoginAsync(
                model.email,
                model.password);

            // Cuenta bloqueada por intentos fallidos: mismo mensaje que el límite por correo.
            if (resultado == ResultadoLogin.Bloqueado)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { message = LimitesSolicitudes.MensajeRechazo });
            }

            if (sesion == null)
            {
                return Unauthorized("Usuario o contraseña incorrectos");
            }

            var usuario = sesion.Datos;
            (usuario.token, usuario.expiraEn) = _tokenService.GenerarToken(usuario, sesion.Sello);

            return Ok(usuario);
        }

        // El JWT no tiene estado en el servidor: cerrar sesión = el cliente descarta el token.
        // (Cambiar la contraseña, el rol o desactivar la cuenta sí revoca los tokens: ver ValidadorSesion.)
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new
            {
                mensaje = "Sesión cerrada correctamente"
            });
        }
    }
}
