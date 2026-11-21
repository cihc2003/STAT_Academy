using STAT_Academy.Web.Models.Contrasena;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace STAT_Academy.Web.Services
{
    public class ApiContrasenaService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiContrasenaService> _logger;

        public ApiContrasenaService(
            HttpClient httpClient,
            ILogger<ApiContrasenaService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> SolicitarRecuperacion(
            SolicitarRecuperacionViewModel model)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/Contrasena/solicitar-recuperacion",
                model
            );

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RestablecerContrasena(
            RestablecerContrasenaViewModel model)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/Contrasena/restablecer",
                model
            );

            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Cambia la contraseña del usuario autenticado y devuelve el mensaje
        /// exacto de la API (por ejemplo, "La contraseña actual es incorrecta.").
        /// Si tuvo éxito trae el token nuevo: el cambio revoca el anterior.
        /// </summary>
        public async Task<(bool exitoso, string mensaje, string? token, DateTime? expiraEn)> CambiarContrasena(
            CambiarContrasenaViewModel model)
        {
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.PostAsJsonAsync(
                    "api/Contrasena/cambiar",
                    new
                    {
                        model.contrasenaActual,
                        model.nuevaContrasena,
                        model.confirmarContrasena
                    }
                );
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "No se pudo contactar la API para cambiar la contraseña.");
                return (false, "No se pudo contactar el servidor. Intente nuevamente.", null, null);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return (false, "Su sesión expiró. Cierre sesión e inicie sesión nuevamente.", null, null);
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return (false, "Demasiados intentos. Espere unos minutos e intente de nuevo.", null, null);
            }

            var contenido = await response.Content.ReadAsStringAsync();
            var mensaje = LeerMensaje(contenido);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Cambio de contraseña rechazado por la API: {Status} {Mensaje}",
                    (int)response.StatusCode, mensaje);
            }

            var (token, expiraEn) = response.IsSuccessStatusCode ? LeerToken(contenido) : (null, null);

            return (
                response.IsSuccessStatusCode,
                mensaje ?? (response.IsSuccessStatusCode
                    ? "La contraseña fue cambiada correctamente."
                    : "No se pudo cambiar la contraseña. Intente nuevamente."),
                token,
                expiraEn
            );
        }

        private static (string? token, DateTime? expiraEn) LeerToken(string contenido)
        {
            try
            {
                using var documento = JsonDocument.Parse(contenido);
                var raiz = documento.RootElement;

                if (raiz.ValueKind == JsonValueKind.Object
                    && raiz.TryGetProperty("token", out var token) && token.ValueKind == JsonValueKind.String
                    && raiz.TryGetProperty("expiraEn", out var expira) && expira.TryGetDateTime(out var expiraEn))
                {
                    return (token.GetString(), expiraEn);
                }
            }
            catch (JsonException)
            {
            }

            return (null, null);
        }

        // Lee { "message": "..." } o, si es un error de validación (ProblemDetails),
        // el primer mensaje de "errors".
        private static string? LeerMensaje(string contenido)
        {

            if (string.IsNullOrWhiteSpace(contenido))
                return null;

            try
            {
                using var documento = JsonDocument.Parse(contenido);
                var raiz = documento.RootElement;

                if (raiz.ValueKind != JsonValueKind.Object)
                    return null;

                if (raiz.TryGetProperty("message", out var message))
                    return message.GetString();

                if (raiz.TryGetProperty("errors", out var errors)
                    && errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var campo in errors.EnumerateObject())
                    {
                        if (campo.Value.ValueKind == JsonValueKind.Array
                            && campo.Value.GetArrayLength() > 0)
                        {
                            return campo.Value[0].GetString();
                        }
                    }
                }
            }
            catch (JsonException)
            {
            }

            return null;
        }
    }
}
