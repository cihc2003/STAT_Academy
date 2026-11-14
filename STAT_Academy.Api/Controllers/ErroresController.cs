using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Errores;

namespace STAT_Academy.Api.Controllers
{
    /// <summary>
    /// Recibe los errores no controlados de la Web para avisar a los administradores
    /// con el mismo límite de envío que los errores de la API. Exige sesión (política por defecto).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ErroresController : ControllerBase
    {
        private readonly ErrorAlertaService _alertas;
        private readonly ILogger<ErroresController> _logger;

        public ErroresController(ErrorAlertaService alertas, ILogger<ErroresController> logger)
        {
            _alertas = alertas;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult Reportar([FromBody] ReporteErrorRequest request)
        {
            _logger.LogWarning("Error reportado por la Web: {Tipo} en {Ruta}. TraceId: {TraceId}",
                request.Tipo, request.Ruta, request.TraceId);

            _alertas.Avisar(new AvisoError(
                Origen: "Web",
                Tipo: request.Tipo,
                Mensaje: request.Mensaje ?? "",
                Ruta: request.Ruta,
                TraceId: request.TraceId,
                FechaUtc: DateTime.UtcNow));

            return Accepted(new { message = "Error reportado." });
        }

        public class ReporteErrorRequest
        {
            [Required, StringLength(200)]
            public string Tipo { get; set; } = "";

            [StringLength(1000)]
            public string? Mensaje { get; set; }

            [Required, StringLength(300)]
            public string Ruta { get; set; } = "";

            [Required, StringLength(100)]
            public string TraceId { get; set; } = "";
        }
    }
}
