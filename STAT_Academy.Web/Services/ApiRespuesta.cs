using System.Net;
using System.Text.Json;

namespace STAT_Academy.Web.Services
{
    /// <summary>Convierte una respuesta de la API en (éxito, mensaje legible para el usuario).</summary>
    public static class ApiRespuesta
    {
        public static async Task<(bool exitoso, string mensaje)> Leer(HttpResponseMessage response)
        {
            var exitoso = response.IsSuccessStatusCode;
            var porDefecto = exitoso
                ? "Operación realizada correctamente."
                : response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "Su sesión expiró. Inicie sesión nuevamente.",
                    HttpStatusCode.Forbidden => "No tiene permiso para realizar esta acción.",
                    HttpStatusCode.NotFound => "El recurso no existe.",
                    HttpStatusCode.RequestEntityTooLarge => "El archivo supera el tamaño máximo permitido.",
                    _ => "No se pudo procesar la solicitud."
                };

            var contenido = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(contenido))
                return (exitoso, porDefecto);

            try
            {
                using var documento = JsonDocument.Parse(contenido);
                var raiz = documento.RootElement;

                if (raiz.ValueKind == JsonValueKind.Object)
                {
                    if (raiz.TryGetProperty("message", out var mensaje) && mensaje.ValueKind == JsonValueKind.String)
                        return (exitoso, mensaje.GetString() ?? porDefecto);

                    // Errores de validación automáticos (ProblemDetails)
                    if (raiz.TryGetProperty("errors", out var errores) && errores.ValueKind == JsonValueKind.Object)
                    {
                        var primero = errores.EnumerateObject()
                            .SelectMany(e => e.Value.ValueKind == JsonValueKind.Array ? e.Value.EnumerateArray() : Enumerable.Empty<JsonElement>())
                            .Select(e => e.GetString())
                            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));

                        if (primero != null)
                            return (exitoso, primero);
                    }
                }
            }
            catch (JsonException)
            {
            }

            return (exitoso, porDefecto);
        }
    }
}
