using STAT_Academy.Web.Models.Usuarios;
using System.Net.Http.Json;

public class ApiUsuarioService
{
    private readonly HttpClient _httpClient;

    public ApiUsuarioService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("STATApi");
    }

    public async Task<List<UsuarioResponse>?> GetUsuarios()
    {
        return await _httpClient.GetFromJsonAsync<List<UsuarioResponse>>("api/Usuario");
    }

    public async Task<UsuarioResponse?> GetUsuarioById(int id)
    {
        return await _httpClient.GetFromJsonAsync<UsuarioResponse>($"api/Usuario/{id}");
    }

    public async Task<List<UsuarioResponse>?> GetUsuariosActivos()
    {
        return await _httpClient.GetFromJsonAsync<List<UsuarioResponse>>("api/Usuario/activos");
    }

    public async Task<UsuarioResponse?> RegistrarUsuario(RegisterUsuarioRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/Usuario/Registrar", request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<UsuarioResponse>();
    }

    public async Task<UsuarioResponse?> DesactivarUsuario(int id)
    {
        var response = await _httpClient.PatchAsync($"api/Usuario/{id}/desactivar", null);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<UsuarioResponse>();
    }
}