using STAT_Academy.Web.Models.Usuarios;
using STAT_Academy.Web.Models.Cuenta;
using System.Net;
using System.Net.Http.Json;

namespace STAT_Academy.Web.Services
{
    public class ApiUserGateway
    {
        private readonly HttpClient _httpClient;

        public ApiUserGateway(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Inicia sesión en la API. Si falla, <c>error</c> trae el motivo para mostrar
        /// (null = usuario o contraseña incorrectos).
        /// </summary>
        public async Task<(LoginResult? usuario, string? error)> LoginAsync(LoginViewModel model)
        {
            using var response = await _httpClient.PostAsJsonAsync("api/Login", new
            {
                email = model.email,
                password = model.password
            });

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<LoginResult>(), null);

            // 401 = credenciales incorrectas; otros (429 demasiados intentos, 503 base iniciándose...) traen su mensaje.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return (null, null);

            var (_, mensaje) = await ApiRespuesta.Leer(response);
            return (null, mensaje);
        }

        public async Task LogoutAsync()
        {
            await _httpClient.PostAsync("api/Login/logout", null);
        }
    }
}