using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Factura;

namespace STAT_Academy.Api.Controllers
{
    /// <summary>
    /// Facturas y pagos. Cada usuario ve solo lo suyo (la identidad sale del token);
    /// el Admin consulta todo y aprueba o rechaza los comprobantes.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    public class FacturaController : ApiControllerBase
    {
        private readonly FacturaService _service;

        public FacturaController(FacturaService service)
        {
            _service = service;
        }

        // ---------------------------------------------------------------
        // Usuario
        // ---------------------------------------------------------------

        /// <summary>Datos de SINPE Móvil y de la cuenta bancaria para pagar.</summary>
        [HttpGet("instrucciones-pago")]
        public IActionResult InstruccionesPago() => Ok(_service.Instrucciones());

        /// <summary>Facturas del usuario autenticado.</summary>
        [HttpGet("mias")]
        public async Task<IActionResult> MisFacturas() =>
            Ok(await _service.MisFacturas(UsuarioActualId));

        /// <summary>Detalle de una factura propia (Admin: cualquiera), con sus comprobantes.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Detalle(int id)
        {
            var factura = await _service.Detalle(id, UsuarioActualId, EsAdmin);
            return factura == null ? NotFound(new { message = "La factura no existe." }) : Ok(factura);
        }

        /// <summary>Sube el comprobante de pago de una factura propia. Queda pendiente de verificación.</summary>
        [HttpPost("{id:int}/comprobantes")]
        [RequestSizeLimit(FacturaService.MaxBytesPeticion)]
        [RequestFormLimits(MultipartBodyLengthLimit = FacturaService.MaxBytesPeticion)]
        public async Task<IActionResult> EnviarComprobante(
            int id,
            [FromForm] string? metodo,
            [FromForm] string? referencia,
            IFormFile? archivo,
            CancellationToken ct)
        {
            var (pago, error, noExiste) = await _service.EnviarComprobante(
                id, UsuarioActualId, UsuarioActualEmail, metodo, referencia, archivo, ct);

            if (pago == null)
                return noExiste ? NotFound(new { message = error }) : BadRequest(new { message = error });

            return Ok(new
            {
                message = "Recibimos tu comprobante. Te avisaremos por correo cuando lo verifiquemos.",
                pago
            });
        }

        /// <summary>Archivo del comprobante (dueño o Admin).</summary>
        [HttpGet("pagos/{pagoId:int}/comprobante")]
        public async Task<IActionResult> Comprobante(int pagoId, CancellationToken ct)
        {
            var archivo = await _service.AbrirComprobante(pagoId, UsuarioActualId, EsAdmin, ct);
            if (archivo == null)
                return NotFound(new { message = "El comprobante no existe." });

            var (contenido, nombre) = archivo.Value;
            return File(contenido.Contenido, contenido.ContentType ?? "application/octet-stream", nombre);
        }

        // ---------------------------------------------------------------
        // Administración
        // ---------------------------------------------------------------

        /// <summary>Todas las facturas.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet("admin")]
        public async Task<IActionResult> Todas() => Ok(await _service.Todas());

        /// <summary>Comprobantes de pago; ?estado=PENDIENTE|APROBADO|RECHAZADO.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet("admin/pagos")]
        public async Task<IActionResult> Pagos([FromQuery] string? estado) => Ok(await _service.Pagos(estado));

        /// <summary>Cantidad de comprobantes por verificar.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet("admin/pagos/pendientes/cantidad")]
        public async Task<IActionResult> CantidadPendientes() => Ok(await _service.PendientesDeRevision());

        [Authorize(Roles = Roles.Admin)]
        [HttpPut("admin/pagos/{pagoId:int}/aprobar")]
        public async Task<IActionResult> Aprobar(int pagoId)
        {
            var (ok, mensaje) = await _service.Aprobar(pagoId, UsuarioActualId, UsuarioActualEmail);
            return ok ? Ok(new { message = mensaje }) : BadRequest(new { message = mensaje });
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPut("admin/pagos/{pagoId:int}/rechazar")]
        public async Task<IActionResult> Rechazar(int pagoId, [FromBody] RechazarPagoRequest request)
        {
            var (ok, mensaje) = await _service.Rechazar(pagoId, UsuarioActualId, UsuarioActualEmail, request?.observacion);
            return ok ? Ok(new { message = mensaje }) : BadRequest(new { message = mensaje });
        }
    }
}
