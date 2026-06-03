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

        public LoginController(LoginService loginService)
        {
            _loginService = loginService;
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
    }
}