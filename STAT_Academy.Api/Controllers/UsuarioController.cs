using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Auth;
using STAT_Academy.DTOs.Usuarios;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    [Route("api/[controller]")]
    public class UsuarioController : ApiControllerBase
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
        /// <summary>Lista paginada y filtrada para el panel de administración.</summary>
        /// <param name="texto">Busca en el nombre y el correo.</param>
        /// <param name="orden">nombre, nombre_desc, registro, registro_desc, login o login_desc.</param>
        [HttpGet("buscar")]
        public ActionResult<UsuarioPaginaResponse> Buscar(
            [FromQuery] string? texto,
            [FromQuery] int? tipo,
            [FromQuery] bool? estado,
            [FromQuery] string? orden,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamano = 10)
        {
            return Ok(_usuarioService.Buscar(texto, tipo, estado, orden, pagina, tamano));
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
                var newUsuario = _usuarioService.CreateUsuario(request, UsuarioActualEmail);

                return CreatedAtAction(
                    nameof(GetUsuarioById),
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
                var usuarioActualizado = _usuarioService.UpdateUsuario(id, request, UsuarioActualId, UsuarioActualEmail);

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
        public ActionResult<UsuarioResponse> DesactivarUsuario(int id) => CambiarEstado(id, false);

        [HttpPatch("{id}/activar")]
        public ActionResult<UsuarioResponse> ActivarUsuario(int id) => CambiarEstado(id, true);

        private ActionResult<UsuarioResponse> CambiarEstado(int id, bool activo)
        {
            try
            {
                var usuario = _usuarioService.CambiarEstado(id, activo, UsuarioActualId, UsuarioActualEmail);

                return usuario == null
                    ? NotFound(new { message = "Usuario no encontrado." })
                    : Ok(usuario);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}