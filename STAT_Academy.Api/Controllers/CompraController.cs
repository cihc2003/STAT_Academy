using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    /// <summary>Compra del carrito del usuario autenticado. El pago es simulado.</summary>
    [Authorize]
    [Route("api/[controller]")]
    public class CompraController : ApiControllerBase
    {
        private readonly CompraService _service;

        public CompraController(CompraService service)
        {
            _service = service;
        }

        /// <summary>Confirma la compra: crea pago y factura, descuenta stock y vacía el carrito.</summary>
        [Authorize(Roles = Roles.Cliente)]
        [HttpPost]
        public async Task<IActionResult> Comprar()
        {
            var (compra, error) = await _service.Comprar(UsuarioActualId, UsuarioActualEmail);
            return compra == null ? BadRequest(new { message = error }) : Ok(compra);
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
