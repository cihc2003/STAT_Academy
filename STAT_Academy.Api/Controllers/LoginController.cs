using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly LoginService _loginService;
        private readonly AuditoriaService _auditoria;

        public LoginController(LoginService loginService,
                       AuditoriaService auditoria)
        {
            _loginService = loginService;
            _auditoria = auditoria;
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

            return Ok(usuario);
        }
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            _auditoria.Registrar(
                "USUARIO",
                "LOGOUT",
                "Cierre de sesión",
                "usuario"
            );

            return Ok(new
            {
                mensaje = "Sesión cerrada correctamente"
            });
        }
    }
}