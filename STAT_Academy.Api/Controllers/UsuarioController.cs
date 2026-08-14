using Microsoft.AspNetCore.Mvc;
using STAT_Academy.DTOs.Usuarios;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        [HttpGet("tipo/{tipo}")]
        public IActionResult FiltrarPorTipo(int tipo)
        {
            return Ok(_usuarioService.FiltrarPorTipo(tipo));
        }

        [HttpGet("activos")]
        public IActionResult Activos()
        {
            return Ok(_usuarioService.UsuariosActivos());
        }

        [HttpGet("{id}")]
        public ActionResult<UsuarioResponse> GetUsuarioById(int id)
        {
            var usuario = _usuarioService.GetUsuarioById(id);

            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            return Ok(usuario);
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

        [HttpPost("Registrar")]
        public ActionResult<UsuarioResponse> RegisterUsuario(RegisterUsuarioRequest request)
        {
            try
            {
                var newUsuario = _usuarioService.RegisterUsuario(request);
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

        [HttpPut("{id}")]
        public ActionResult<UsuarioResponse> UpdateUsuario(int id, UpdateUsuarioRequest request)
        {
            try
            {
                var usuarioActualizado = _usuarioService.UpdateUsuario(id, request);

                if (usuarioActualizado == null)
                {
                    return NotFound(new { message = "Usuario no encontrado." });
                }

                return Ok(usuarioActualizado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/desactivar")]
        public ActionResult<UsuarioResponse> DesactivarUsuario(int id)
        {
            var usuarioDesactivado = _usuarioService.DesactivarUsuario(id);

            if (usuarioDesactivado == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            return Ok(usuarioDesactivado);
        }

    }
}