using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using STAT_Academy.Api.Auth;

namespace STAT_Academy.Api.Seguridad
{
    /// <summary>
    /// Límites de solicitudes (rate limiting) de las operaciones sensibles. Usa el limitador que trae
    /// ASP.NET Core (sin paquetes extra). Los contadores viven en memoria de cada instancia.
    ///
    /// Ojo: casi todas las peticiones llegan desde el servidor de la Web, así que aquí la IP es la de
    /// la Web. Por eso el límite por IP es amplio (frena a quien llama a la API directamente) y la
    /// protección contra fuerza bruta va por CORREO (<see cref="LimitadorPorCorreo"/>). La Web aplica
    /// además su propio límite por IP del visitante.
    /// </summary>
    public static class LimitesSolicitudes
    {
        /// <summary>Login, registro y recuperación de contraseña: por IP.</summary>
        public const string Anonimo = "anonimo-sensible";

        /// <summary>Cambio de contraseña: por usuario autenticado.</summary>
        public const string CambioContrasena = "cambio-contrasena";

        /// <summary>Reportes de error de la Web (cada uno puede generar un correo).</summary>
        public const string ReporteErrores = "reporte-errores";

        public const string MensajeRechazo =
            "Demasiados intentos. Espere unos minutos e intente de nuevo.";

        public static IServiceCollection AddLimitesSolicitudes(this IServiceCollection services)
        {
            services.AddSingleton<LimitadorPorCorreo>();

            services.AddRateLimiter(opciones =>
            {
                opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                opciones.OnRejected = async (contexto, ct) =>
                {
                    if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                        contexto.HttpContext.Response.Headers.RetryAfter = ((int)espera.TotalSeconds).ToString();

                    await contexto.HttpContext.Response.WriteAsJsonAsync(new { message = MensajeRechazo }, ct);
                };

                opciones.AddPolicy(Anonimo, http => RateLimitPartition.GetFixedWindowLimiter(
                    http.Connection.RemoteIpAddress?.ToString() ?? "sin-ip",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

                opciones.AddPolicy(CambioContrasena, http => RateLimitPartition.GetFixedWindowLimiter(
                    ClaveUsuario(http),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0
                    }));

                opciones.AddPolicy(ReporteErrores, http => RateLimitPartition.GetFixedWindowLimiter(
                    ClaveUsuario(http),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(10),
                        QueueLimit = 0
                    }));
            });

            return services;
        }

        private static string ClaveUsuario(HttpContext http) =>
            http.User.FindFirst(ClaimTiposJwt.UsuarioId)?.Value
            ?? "ip:" + (http.Connection.RemoteIpAddress?.ToString() ?? "sin-ip");
    }

    /// <summary>
    /// Límites por correo electrónico (no dependen de la IP): frenan la fuerza bruta contra una
    /// cuenta y el envío masivo de correos de recuperación a una misma dirección.
    /// </summary>
    public sealed class LimitadorPorCorreo : IDisposable
    {
        // 10 intentos de login cada 15 minutos por cuenta.
        private readonly PartitionedRateLimiter<string> _login = Crear(10, TimeSpan.FromMinutes(15));

        // 3 correos de recuperación por hora por dirección.
        private readonly PartitionedRateLimiter<string> _recuperacion = Crear(3, TimeSpan.FromHours(1));

        public bool PermitirLogin(string? email) => Permitir(_login, email);

        public bool PermitirRecuperacion(string? email) => Permitir(_recuperacion, email);

        private static bool Permitir(PartitionedRateLimiter<string> limitador, string? email)
        {
            using var permiso = limitador.AttemptAcquire(Normalizar(email));
            return permiso.IsAcquired;
        }

        private static string Normalizar(string? email) =>
            (email ?? string.Empty).Trim().ToLowerInvariant();

        private static PartitionedRateLimiter<string> Crear(int permitidos, TimeSpan ventana) =>
            PartitionedRateLimiter.Create<string, string>(clave => RateLimitPartition.GetFixedWindowLimiter(
                clave,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitidos,
                    Window = ventana,
                    QueueLimit = 0
                }));

        public void Dispose()
        {
            _login.Dispose();
            _recuperacion.Dispose();
        }
    }
}
