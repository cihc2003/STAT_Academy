using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace STAT_Academy.Web.Seguridad
{
    /// <summary>
    /// Límites por IP del visitante en los formularios de cuenta. La API también limita, pero allí
    /// todas las peticiones llegan con la IP de este servidor; aquí sí se distingue a cada visitante.
    /// Los valores toleran un aula o una oficina detrás de una sola IP.
    /// </summary>
    public static class LimitesSolicitudesWeb
    {
        public const string Login = "web-login";
        public const string Cuenta = "web-cuenta";

        public const string MensajeRechazo =
            "Demasiados intentos desde esta conexión. Espere unos minutos e intente de nuevo.";

        public static IServiceCollection AddLimitesSolicitudesWeb(this IServiceCollection services)
        {
            services.AddRateLimiter(opciones =>
            {
                opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Formularios: se vuelve a la misma página con el aviso (toast de error del _Layout).
                opciones.OnRejected = (contexto, _) =>
                {
                    var http = contexto.HttpContext;
                    var tempData = http.RequestServices.GetRequiredService<ITempDataDictionaryFactory>().GetTempData(http);
                    tempData["Error"] = MensajeRechazo;
                    tempData.Save();

                    http.Response.StatusCode = StatusCodes.Status303SeeOther;
                    http.Response.Headers.Location = http.Request.PathBase + http.Request.Path + http.Request.QueryString;
                    return ValueTask.CompletedTask;
                };

                // 30 intentos de login por minuto por IP.
                opciones.AddPolicy(Login, http => PorIp(http, 30, TimeSpan.FromMinutes(1)));

                // Registro, recuperación y restablecimiento: 10 cada 10 minutos por IP.
                opciones.AddPolicy(Cuenta, http => PorIp(http, 10, TimeSpan.FromMinutes(10)));
            });

            return services;
        }

        private static RateLimitPartition<string> PorIp(HttpContext http, int permitidos, TimeSpan ventana) =>
            RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "sin-ip",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitidos,
                    Window = ventana,
                    QueueLimit = 0
                });
    }
}
