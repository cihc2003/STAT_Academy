using STAT_Academy.Web.Models.Usuarios;
using STAT_Academy.Web.Models.Cuenta;
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

        public async Task<UsuarioResponse?> LoginAsync(LoginViewModel model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/Login", new
            {
                email = model.email,
                password = model.password
            }); 

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<UsuarioResponse>();
        }

        public async Task LogoutAsync()
        {
            await _httpClient.PostAsync("api/Login/logout", null);
        }
    }
}