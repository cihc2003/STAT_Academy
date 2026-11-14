using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    /// <summary>
    /// Compras del usuario autenticado. La factura queda pendiente hasta que un administrador
    /// aprueba el comprobante de pago (ver FacturaController).
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    public class CompraController : ApiControllerBase
    {
        private readonly CompraService _service;

        public CompraController(CompraService service)
        {
            _service = service;
        }

        /// <summary>Confirma la compra del carrito: crea la factura pendiente, reserva el stock y vacía el carrito.</summary>
        [Authorize(Roles = Roles.Cliente)]
        [HttpPost]
        public async Task<IActionResult> Comprar()
        {
            var (compra, error) = await _service.Comprar(UsuarioActualId, UsuarioActualEmail);
            return compra == null ? BadRequest(new { message = error }) : Ok(compra);
        }

        /// <summary>Compra de un curso de pago. Devuelve la factura creada (o la pendiente que ya existía).</summary>
        [Authorize(Roles = Roles.Cliente)]
        [HttpPost("curso/{cursoId:int}")]
        public async Task<IActionResult> ComprarCurso(int cursoId)
        {
            var (facturaId, error, existente) = await _service.ComprarCurso(UsuarioActualId, UsuarioActualEmail, cursoId);

            if (facturaId == null)
                return BadRequest(new { message = error });

            return Ok(new
            {
                facturaId,
                existente,
                message = existente
                    ? "Ya tenías una compra pendiente de este curso. Completa el pago para obtener acceso."
                    : "Compra registrada. Te enviamos el comprobante por correo."
            });
        }

        /// <summary>Comprobante de una compra propia (Admin: cualquiera).</summary>
        [HttpGet("{facturaId:int}")]
        public IActionResult Obtener(int facturaId)
        {
            var compra = _service.ObtenerCompra(facturaId, UsuarioActualId, EsAdmin);
            return compra == null ? NotFound(new { message = "La compra no existe." }) : Ok(compra);
        }
    }
}
