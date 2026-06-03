using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.DTOs.Usuarios;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly UsuarioService _usuarioService;

        public UsuarioController(UsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<UsuarioResponse>> GetUsuario()
        {
            return Ok(_usuarioService.GetUsuarios());
        }

        [HttpPost]
        public ActionResult<UsuarioResponse> CreateUsuario(CreateUsuarioRequest request)
        {
            try
            {
                var newUsuario = _usuarioService.CreateUsuario(request);

                return CreatedAtAction(
                    nameof(GetUsuario),
                    new { id = newUsuario.id },
                    newUsuario);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}