using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Carrito;

namespace STAT_Academy.Api.Controllers
{
    // Las rutas conservan {usuarioId} por compatibilidad con la Web, pero solo
    // el dueño del carrito (o un Admin) puede usarlas.
    [Authorize]
    [Route("api/[controller]")]
    public class CarritoController : ApiControllerBase
    {
        private readonly CarritoService _service;

        public CarritoController(CarritoService service)
        {
            _service = service;
        }

        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> GetCarrito(int usuarioId)
        {
            if (!PuedeActuarSobre(usuarioId))
                return Forbid();

            return Ok(await _service.GetCarritoAsync(usuarioId));
        }

        [HttpPost("agregar")]
        public async Task<IActionResult> Agregar([FromBody] AgregarAlCarritoRequest request)
        {
            // Se ignora el usuarioId del cuerpo: el carrito siempre es el del usuario autenticado.
            request.usuarioId = UsuarioActualId;

            try
            {
                return Ok(await _service.AgregarItemAsync(request));
            }
            catch (InvalidOperationException ex)
            {
                // Reglas del carrito (stock, producto no disponible). Cualquier otro error va al
                // manejador global, que no muestra detalles internos.
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPut("{detalleId}/usuario/{usuarioId}")]
        public async Task<IActionResult> ActualizarCantidad(int detalleId, int usuarioId, [FromBody] ActualizarCantidadRequest request)
        {
            if (!PuedeActuarSobre(usuarioId))
                return Forbid();

            try
            {
                return Ok(await _service.ActualizarCantidadAsync(detalleId, usuarioId, request.cantidad));
            }
            catch (InvalidOperationException ex)
            {
                // Reglas del carrito (stock, producto no disponible). Cualquier otro error va al
                // manejador global, que no muestra detalles internos.
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpDelete("{detalleId}/usuario/{usuarioId}")]
        public async Task<IActionResult> Eliminar(int detalleId, int usuarioId)
        {
            if (!PuedeActuarSobre(usuarioId))
                return Forbid();

            return Ok(await _service.EliminarItemAsync(detalleId, usuarioId));
        }
    }
}
