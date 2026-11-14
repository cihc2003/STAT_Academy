using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Auth;
using STAT_Academy.DTOs.Usuarios;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly UsuarioService _usuarioService;
        private readonly CorreoService _correoService;
        private readonly ILogger<UsuarioController> _logger;

        public UsuarioController(
            UsuarioService usuarioService,
            CorreoService correoService,
            ILogger<UsuarioController> logger)
        {
            _usuarioService = usuarioService;
            _correoService = correoService;
            _logger = logger;
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

        [AllowAnonymous]
        [HttpPost("Registrar")]
        public async Task<ActionResult<UsuarioResponse>> RegisterUsuario(RegisterUsuarioRequest request)
        {
            UsuarioResponse newUsuario;

            try
            {
                newUsuario = _usuarioService.RegisterUsuario(request);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            // La cuenta ya existe: si el correo falla, no se revierte el registro.
            try
            {
                await _correoService.EnviarConfirmacionRegistro(
                    newUsuario.email,
                    newUsuario.nombre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudo enviar el correo de confirmación de registro al usuario {UsuarioId}.",
                    newUsuario.id);
            }

            return CreatedAtAction(
                nameof(GetUsuarioById),
                new { id = newUsuario.id },
                newUsuario);
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

        [HttpPatch("{id}/activar")]
        public ActionResult<UsuarioResponse> ActivarUsuario(int id)
        {
            var usuarioActivado = _usuarioService.ActivarUsuario(id);

            if (usuarioActivado == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            return Ok(usuarioActivado);
        }

    }
}