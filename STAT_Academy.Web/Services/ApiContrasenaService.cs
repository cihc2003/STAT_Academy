using STAT_Academy.Web.Models.Contrasena;
using System.Net.Http.Json;

namespace STAT_Academy.Web.Services
{
    public class ApiContrasenaService
    {
        private readonly HttpClient _httpClient;

        public ApiContrasenaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
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

        public async Task<bool> CambiarContrasena(
            CambiarContrasenaViewModel model)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/Contrasena/cambiar",
                model
            );

            return response.IsSuccessStatusCode;
        }
    }
}