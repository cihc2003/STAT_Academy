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

    /// <summary>Página de usuarios filtrada y ordenada en la API.</summary>
    public async Task<UsuarioPaginaResponse> BuscarAsync(UsuarioFiltros f)
    {
        var query = new List<string>
        {
            $"pagina={f.Pagina}",
            $"tamano={f.Tamano}"
        };

        if (!string.IsNullOrWhiteSpace(f.Buscar)) query.Add("texto=" + Uri.EscapeDataString(f.Buscar.Trim()));
        if (f.Tipo != null) query.Add($"tipo={f.Tipo}");
        if (f.Activo != null) query.Add($"estado={(f.Activo.Value ? "true" : "false")}");
        if (!string.IsNullOrWhiteSpace(f.Orden)) query.Add("orden=" + Uri.EscapeDataString(f.Orden));

        return await _httpClient.GetFromJsonAsync<UsuarioPaginaResponse>("api/Usuario/buscar?" + string.Join("&", query))
               ?? new UsuarioPaginaResponse();
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

    /// <summary>Alta desde el panel (solo Admin): respeta el tipo elegido.</summary>
    public async Task<(bool exitoso, string mensaje)> CrearUsuarioAsync(CreateUsuarioRequest request)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/Usuario", request);
        return await ApiRespuesta.Leer(response);
    }

    /// <summary>Registro público (siempre crea un Cliente y envía el correo de bienvenida).</summary>
    /// <returns>El usuario creado, o null y el motivo (p. ej. correo ya registrado o demasiados intentos).</returns>
    public async Task<(UsuarioResponse? usuario, string mensaje)> RegistrarUsuarioAsync(RegisterUsuarioRequest request)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/Usuario/Registrar", request);

        if (!response.IsSuccessStatusCode)
        {
            var (_, mensaje) = await ApiRespuesta.Leer(response);
            return (null, mensaje);
        }

        return (await response.Content.ReadFromJsonAsync<UsuarioResponse>(), string.Empty);
    }

    public async Task<(bool exitoso, string mensaje)> ActualizarUsuarioAsync(int id, UpdateUsuarioRequest request)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/Usuario/{id}", request);
        return await ApiRespuesta.Leer(response);
    }

    public async Task<(bool exitoso, string mensaje)> CambiarEstadoAsync(int id, bool activo)
    {
        using var response = await _httpClient.PatchAsync($"api/Usuario/{id}/{(activo ? "activar" : "desactivar")}", null);
        return await ApiRespuesta.Leer(response);
    }
}
