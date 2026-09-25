using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Carrito;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CarritoController : ControllerBase
    {
        private readonly CarritoService _service;

        public CarritoController(CarritoService service)
        {
            _service = service;
        }

        [HttpGet("usuario/{usuarioId}")]
        public IActionResult GetCarrito(int usuarioId)
        {
            return Ok(_service.GetCarrito(usuarioId));
        }

        [HttpPost("agregar")]
        public IActionResult Agregar([FromBody] AgregarAlCarritoRequest request)
        {
            try
            {
                return Ok(_service.AgregarItem(request));
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPut("{detalleId}/usuario/{usuarioId}")]
        public IActionResult ActualizarCantidad(int detalleId, int usuarioId, [FromBody] ActualizarCantidadRequest request)
        {
            try
            {
                return Ok(_service.ActualizarCantidad(detalleId, usuarioId, request.cantidad));
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpDelete("{detalleId}/usuario/{usuarioId}")]
        public IActionResult Eliminar(int detalleId, int usuarioId)
        {
            return Ok(_service.EliminarItem(detalleId, usuarioId));
        }
    }
}