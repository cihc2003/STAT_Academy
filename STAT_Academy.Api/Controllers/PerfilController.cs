using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Usuarios;

namespace STAT_Academy.Api.Controllers
{
    /// <summary>
    /// Perfil del usuario autenticado (cualquier rol). Solo actúa sobre la cuenta del token:
    /// no recibe ids. El correo no se edita.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    public class PerfilController : ApiControllerBase
    {
        private readonly UsuarioService _usuarioService;

        public PerfilController(UsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public ActionResult<PerfilResponse> Obtener()
        {
            var perfil = _usuarioService.ObtenerPerfil(UsuarioActualId);
            return perfil == null ? NotFound(new { message = "La cuenta no existe." }) : Ok(perfil);
        }

        /// <summary>Cambia el nombre propio (2–150 caracteres).</summary>
        [HttpPut]
        public ActionResult<PerfilResponse> Actualizar(ActualizarPerfilRequest request)
        {
            try
            {
                var perfil = _usuarioService.ActualizarNombrePropio(UsuarioActualId, request.nombre, UsuarioActualEmail);
                return perfil == null ? NotFound(new { message = "La cuenta no existe o está desactivada." }) : Ok(perfil);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
