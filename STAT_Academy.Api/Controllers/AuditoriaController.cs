using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditoriaController : ControllerBase
    {
        private readonly AuditoriaService _service;

        public AuditoriaController(AuditoriaService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var data = _service.GetAll();

            if (data == null || data.Count == 0)
                return NoContent();

            return Ok(data);
        }

        [HttpGet("accion/{accion}")]
        public IActionResult FiltrarPorAccion(string accion)
        {
            var data = _service.FiltrarPorAccion(accion);

            if (!data.Any())
                return NotFound("No hay registros con esa acción");

            return Ok(data);
        }

        [HttpGet("entidad/{entidad}")]
        public IActionResult FiltrarPorEntidad(string entidad)
        {
            var data = _service.FiltrarPorEntidad(entidad);

            if (!data.Any())
                return NotFound("No hay registros para esa entidad");

            return Ok(data);
        }
        [HttpGet("producto/{id}")]
        public IActionResult PorProducto(int id)
        {
            return Ok(_service.FiltrarPorProducto(id));
        }
    }
}
