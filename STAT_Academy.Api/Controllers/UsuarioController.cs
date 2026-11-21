using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using STAT_Academy.Api.Seguridad;
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
        public async Task<ActionResult<IEnumerable<UsuarioResponse>>> GetUsuario()
        {
            return Ok(await _usuarioService.GetUsuariosAsync());
        }
        /// <summary>Lista paginada y filtrada para el panel de administración.</summary>
        /// <param name="texto">Busca en el nombre y el correo.</param>
        /// <param name="orden">nombre, nombre_desc, registro, registro_desc, login o login_desc.</param>
        [HttpGet("buscar")]
        public async Task<ActionResult<UsuarioPaginaResponse>> Buscar(
            [FromQuery] string? texto,
            [FromQuery] int? tipo,
            [FromQuery] bool? estado,
            [FromQuery] string? orden,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamano = 10)
        {
            return Ok(await _usuarioService.BuscarAsync(texto, tipo, estado, orden, pagina, tamano));
        }

        [HttpGet("tipo/{tipo}")]
        public async Task<IActionResult> FiltrarPorTipo(int tipo)
        {
            return Ok(await _usuarioService.FiltrarPorTipoAsync(tipo));
        }

        [HttpGet("activos")]
        public async Task<IActionResult> Activos()
        {
            return Ok(await _usuarioService.UsuariosActivosAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioResponse>> GetUsuarioById(int id)
        {
            var usuario = await _usuarioService.GetUsuarioByIdAsync(id);

            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            return Ok(usuario);
        }

        [HttpPost]
        public async Task<ActionResult<UsuarioResponse>> CreateUsuario(CreateUsuarioRequest request)
        {
            try
            {
                var newUsuario = await _usuarioService.CreateUsuarioAsync(request, UsuarioActualEmail);

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
        [EnableRateLimiting(LimitesSolicitudes.Anonimo)]
        public async Task<ActionResult<UsuarioResponse>> RegisterUsuario(RegisterUsuarioRequest request)
        {
            UsuarioResponse newUsuario;

            try
            {
                newUsuario = await _usuarioService.RegisterUsuarioAsync(request);
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
        public async Task<ActionResult<UsuarioResponse>> UpdateUsuario(int id, UpdateUsuarioRequest request)
        {
            try
            {
                var usuarioActualizado = await _usuarioService.UpdateUsuarioAsync(id, request, UsuarioActualId, UsuarioActualEmail);

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
        public Task<ActionResult<UsuarioResponse>> DesactivarUsuario(int id) => CambiarEstado(id, false);

        [HttpPatch("{id}/activar")]
        public Task<ActionResult<UsuarioResponse>> ActivarUsuario(int id) => CambiarEstado(id, true);

        private async Task<ActionResult<UsuarioResponse>> CambiarEstado(int id, bool activo)
        {
            try
            {
                var usuario = await _usuarioService.CambiarEstadoAsync(id, activo, UsuarioActualId, UsuarioActualEmail);

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