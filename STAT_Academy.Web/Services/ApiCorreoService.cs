using STAT_Academy.Web.Models.Correo;
using System.Net.Http.Json;
using System.Text.Json;

namespace STAT_Academy.Web.Services
{
    public class ApiCorreoService
    {
        private readonly HttpClient _httpClient;

        public ApiCorreoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(bool exitoso, string mensaje)>
            SolicitarCambio(
                SolicitarCambioCorreoViewModel model)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/Correo/solicitar-cambio",
                model
            );

            return await ProcesarRespuesta(response);
        }

        public async Task<(bool exitoso, string mensaje)>
            ConfirmarCambio(
                ConfirmarCambioCorreoViewModel model)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/Correo/confirmar-cambio",
                model
            );

            return await ProcesarRespuesta(response);
        }

        private static async Task<(bool exitoso, string mensaje)>
            ProcesarRespuesta(HttpResponseMessage response)
        {
            var contenido = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(contenido))
            {
                return (
                    response.IsSuccessStatusCode,
                    response.IsSuccessStatusCode
                        ? "Operación realizada correctamente."
                        : "No se pudo procesar la solicitud."
                );
            }

            try
            {
                using var documento = JsonDocument.Parse(contenido);

                var mensaje = documento.RootElement
                    .GetProperty("message")
                    .GetString();

                return (
                    response.IsSuccessStatusCode,
                    mensaje ?? "No se recibió un mensaje de la API."
                );
            }
            catch (JsonException)
            {
                return (
                    response.IsSuccessStatusCode,
                    response.IsSuccessStatusCode
                        ? "Operación realizada correctamente."
                        : "No se pudo procesar la solicitud."
                );
            }
        }
    }
}