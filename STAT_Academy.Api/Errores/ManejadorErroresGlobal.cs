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

            // Base de datos no disponible por un momento (p. ej. Azure SQL serverless reanudándose):
            // 503 con aviso de reintentar y sin correo a los administradores, porque es esperado.
            if (EsBaseDeDatosNoDisponible(exception))
            {
                _logger.LogWarning(exception, "Base de datos no disponible en {Ruta}. TraceId: {TraceId}", ruta, traceId);
                httpContext.Response.Headers.RetryAfter = "30";
                return await Escribir(httpContext, exception, StatusCodes.Status503ServiceUnavailable,
                    "El servicio se está iniciando. Intente de nuevo en unos segundos.");
            }

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

        /// <summary>
        /// Errores de SQL que indican que la base no está disponible por ahora:
        /// -2 tiempo de espera, 40613 base no disponible (reanudándose), 40197 / 40501 / 49918-49920 servicio ocupado.
        /// Se revisan las excepciones internas porque EF Core las envuelve (p. ej. RetryLimitExceededException).
        /// </summary>
        private static bool EsBaseDeDatosNoDisponible(Exception exception)
        {
            for (var ex = exception; ex != null; ex = ex.InnerException)
            {
                if (ex is Microsoft.Data.SqlClient.SqlException sql
                    && sql.Errors.Cast<Microsoft.Data.SqlClient.SqlError>().Any(e =>
                        e.Number is -2 or 40613 or 40197 or 40501 or 49918 or 49919 or 49920))
                    return true;
            }

            return false;
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
