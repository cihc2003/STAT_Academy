using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Models.Facturas;
using STAT_Academy.Web.Services;

namespace STAT_Academy.Web.Controllers
{
    /// <summary>
    /// "Mis facturas y pagos": compras del usuario, datos para pagar y envío del comprobante.
    /// Qué factura puede ver cada quien lo decide la API con el token (no se envía el id del usuario).
    /// </summary>
    [Authorize]
    public class FacturasController : Controller
    {
        private readonly ApiFacturaService _api;

        public FacturasController(ApiFacturaService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var facturas = await _api.MisFacturas() ?? [];
            return View(facturas);
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var factura = await _api.Detalle(id);
            if (factura == null)
                return NotFound();

            // Los administradores revisan desde su panel (con aprobar / rechazar).
            var usuarioActual = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (User.IsInRole("Admin") && factura.usuarioId.ToString() != usuarioActual)
                return RedirectToAction("Invoice", "Admin", new { id });

            ViewBag.Formulario = new EnviarComprobanteViewModel { FacturaId = id };
            return View(factura);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(EnviarComprobanteViewModel.MaxBytesPeticion)]
        [RequestFormLimits(MultipartBodyLengthLimit = EnviarComprobanteViewModel.MaxBytesPeticion)]
        public async Task<IActionResult> EnviarComprobante(EnviarComprobanteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                                    ?? "Revisa los datos del comprobante.";
                return RedirectToAction(nameof(Detalle), new { id = model.FacturaId });
            }

            var (exitoso, mensaje) = await _api.EnviarComprobante(model);
            TempData[exitoso ? "Success" : "Error"] = mensaje;
            return RedirectToAction(nameof(Detalle), new { id = model.FacturaId });
        }

        /// <summary>
        /// Muestra (o descarga, con descargar=true) un comprobante enviado. Lo usan el dueño y el Admin;
        /// la API rechaza a cualquier otro.
        /// </summary>
        public async Task<IActionResult> Comprobante(int id, bool descargar, CancellationToken ct)
        {
            var respuesta = await _api.DescargarComprobante(id, ct);
            if (respuesta == null)
                return NotFound();

            HttpContext.Response.RegisterForDispose(respuesta);

            // Archivo subido por un usuario: que el navegador no adivine el tipo ni ejecute nada.
            // (El visor de PDF del navegador no funciona con CSP sandbox; el PDF ya se validó por su firma.)
            var tipo = respuesta.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            if (!tipo.StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase))
                Response.Headers["Content-Security-Policy"] = "sandbox; default-src 'none'; img-src 'self'; style-src 'unsafe-inline'";

            var nombre = respuesta.Content.Headers.ContentDisposition?.FileNameStar
                         ?? respuesta.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                         ?? $"comprobante-{id}";
            var contenido = await respuesta.Content.ReadAsStreamAsync(ct);

            return descargar ? File(contenido, tipo, nombre) : File(contenido, tipo);
        }

        /// <summary>Compra de un curso de pago: crea la factura y lleva a la página para pagar.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> ComprarCurso(int cursoId)
        {
            var (facturaId, mensaje) = await _api.ComprarCurso(cursoId);

            if (facturaId == null)
            {
                TempData["Error"] = mensaje;
                return RedirectToAction("Details", "Curso", new { id = cursoId });
            }

            TempData["Success"] = mensaje;
            return RedirectToAction(nameof(Detalle), new { id = facturaId });
        }
    }
}
