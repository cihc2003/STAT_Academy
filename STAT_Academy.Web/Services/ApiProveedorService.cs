using System.Net.Http.Json;
using STAT_Academy.Web.Models.Proveedor;

namespace STAT_Academy.Web.Services;

public class ApiProveedorService
{
    private readonly HttpClient _httpClient;

    public ApiProveedorService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProveedorResponse>?> GetAllAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<ProveedorResponse>>("api/Proveedor");
    }

    public async Task<ProveedorResponse?> GetByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<ProveedorResponse>($"api/Proveedor/{id}");
    }

    public async Task<ProveedorResponse?> CrearAsync(ProveedorCreateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/Proveedor", request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ProveedorResponse>();
    }

    public async Task<ProveedorResponse?> EditarAsync(int id, ProveedorCreateRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/Proveedor/{id}", request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ProveedorResponse>();
    }

    public async Task<ProveedorResponse?> DesactivarAsync(int id)
    {
        var response = await _httpClient.PatchAsync($"api/Proveedor/desactivar/{id}", null);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ProveedorResponse>();
    }
}
