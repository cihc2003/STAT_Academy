using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    [Route("api/[controller]")]
    public class AuditoriaController : ControllerBase
    {
        private readonly AuditoriaService _service;

        public AuditoriaController(AuditoriaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();

            if (data == null || data.Count == 0)
                return NoContent();

            return Ok(data);
        }

        [HttpGet("accion/{accion}")]
        public async Task<IActionResult> FiltrarPorAccion(string accion)
        {
            var data = await _service.FiltrarPorAccionAsync(accion);

            if (!data.Any())
                return NotFound("No hay registros con esa acción");

            return Ok(data);
        }

        [HttpGet("entidad/{entidad}")]
        public async Task<IActionResult> FiltrarPorEntidad(string entidad)
        {
            var data = await _service.FiltrarPorEntidadAsync(entidad);

            if (!data.Any())
                return NotFound("No hay registros para esa entidad");

            return Ok(data);
        }
        [HttpGet("producto/{id}")]
        public async Task<IActionResult> PorProducto(int id)
        {
            return Ok(await _service.FiltrarPorProductoAsync(id));
        }
    }
}
