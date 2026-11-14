using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace STAT_Academy.Api.Errores
{
    /// <summary>
    /// Atrapa las excepciones no controladas: las registra, avisa a los administradores
    /// (con límite de envío) y responde un mensaje genérico sin detalles internos.
    /// </summary>
    public class ManejadorErroresGlobal : IExceptionHandler
    {
        public const string MensajeGenerico =
            "Ocurrió un error inesperado. Intente de nuevo más tarde; si el problema continúa, " +
            "comuníquese con soporte e indique el código del error.";

        private readonly IProblemDetailsService _problemDetails;
        private readonly ErrorAlertaService _alertas;
        private readonly ILogger<ManejadorErroresGlobal> _logger;

        public ManejadorErroresGlobal(
            IProblemDetailsService problemDetails,
            ErrorAlertaService alertas,
            ILogger<ManejadorErroresGlobal> logger)
        {
            _problemDetails = problemDetails;
            _alertas = alertas;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // El cliente canceló la petición: no es un error del sistema.
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            {
                httpContext.Response.StatusCode = 499;
                return true;
            }

            // Petición mal formada (cuerpo inválido, demasiado grande, etc.).
            if (exception is BadHttpRequestException peticionInvalida)
            {
                return await Escribir(httpContext, exception, peticionInvalida.StatusCode,
                    "La solicitud no es válida.");
            }

            var traceId = httpContext.TraceIdentifier;
            var ruta = $"{httpContext.Request.Method} {httpContext.Request.Path}";

            _logger.LogError(exception, "Error no controlado en {Ruta}. TraceId: {TraceId}", ruta, traceId);

            _alertas.Avisar(new AvisoError(
                Origen: "API",
                Tipo: exception.GetType().FullName ?? exception.GetType().Name,
                Mensaje: exception.Message,
                Ruta: ruta,
                TraceId: traceId,
                FechaUtc: DateTime.UtcNow));

            return await Escribir(httpContext, exception, StatusCodes.Status500InternalServerError, MensajeGenerico);
        }

        private async ValueTask<bool> Escribir(HttpContext httpContext, Exception exception, int estado, string mensaje)
        {
            httpContext.Response.StatusCode = estado;

            var problema = new ProblemDetails
            {
                Status = estado,
                Title = mensaje,
            };
            // Misma forma que el resto de la API: { "message": "..." }
            problema.Extensions["message"] = mensaje;

            return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = problema
            });
        }
    }
}
