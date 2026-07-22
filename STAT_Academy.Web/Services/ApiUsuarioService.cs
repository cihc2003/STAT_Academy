using System.Net.Http.Json;
using STAT_Academy.Web.Models.Usuarios;

namespace STAT_Academy.Web.Services;

public class ApiUsuarioService
{
    private readonly HttpClient _httpClient;

    public ApiUsuarioService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<UsuarioResponse>?> GetUsuariosAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<UsuarioResponse>>("api/Usuario");
    }

    public async Task<UsuarioResponse?> GetUsuarioByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<UsuarioResponse>($"api/Usuario/{id}");
    }

    public async Task<List<UsuarioResponse>?> FiltrarPorTipoAsync(int tipo)
    {
        return await _httpClient.GetFromJsonAsync<List<UsuarioResponse>>($"api/Usuario/tipo/{tipo}");
    }

    public async Task<List<UsuarioResponse>?> GetUsuariosActivosAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<UsuarioResponse>>("api/Usuario/activos");
    }

    public async Task<UsuarioResponse?> CrearUsuarioAsync(CreateUsuarioRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/Usuario", request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<UsuarioResponse>();
    }

    public async Task<UsuarioResponse?> RegistrarUsuarioAsync(RegisterUsuarioRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/Usuario/Registrar", request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<UsuarioResponse>();
    }

    public async Task<UsuarioResponse?> ActualizarUsuarioAsync(int id, UpdateUsuarioRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/Usuario/{id}", request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<UsuarioResponse>();
    }

    public async Task<UsuarioResponse?> DesactivarUsuarioAsync(int id)
    {
        var response = await _httpClient.PatchAsync($"api/Usuario/{id}/desactivar", null);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<UsuarioResponse>();
    }
}