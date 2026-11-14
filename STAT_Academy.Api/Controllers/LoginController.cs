using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class LoginController : ApiControllerBase
    {
        private readonly LoginService _loginService;
        private readonly TokenService _tokenService;

        public LoginController(LoginService loginService, TokenService tokenService)
        {
            _loginService = loginService;
            _tokenService = tokenService;
        }

        [HttpPost]
        public IActionResult Login([FromBody] LoginModel model)
        {
            var usuario = _loginService.Login(
                model.email,
                model.password);

            if (usuario == null)
            {
                return Unauthorized("Usuario o contraseña incorrectos");
            }

            (usuario.token, usuario.expiraEn) = _tokenService.GenerarToken(usuario);

            return Ok(usuario);
        }

        // El JWT no tiene estado en el servidor: cerrar sesión = el cliente descarta el token.
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
