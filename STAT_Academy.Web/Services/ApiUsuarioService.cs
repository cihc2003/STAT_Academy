using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using STAT_Academy.Web.Data;
using STAT_Academy.Web.Models.Api;
using STAT_Academy.Web.Services.Mappers;
using STAT_Academy.Web.ViewModels;

namespace STAT_Academy.Web.Services;

public class ApiUsuarioService
{
    private readonly HttpClient _httpClient;

    public ApiUsuarioService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> EstaDisponibleAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync("Usuario/activos");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<UsuarioApiResponse>> ObtenerUsuariosAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<UsuarioApiResponse>>("Usuario") ?? [];
    }

    public async Task<UsuarioApiResponse?> ObtenerUsuarioAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<UsuarioApiResponse>($"Usuario/{id}");
    }

    public async Task<ApiResultado<UsuarioApiResponse>> RegistrarAsync(RegisterViewModel model)
    {
        var request = new RegistroUsuarioApiRequest(model.Email, model.FullName, model.Password);
        using var response = await _httpClient.PostAsJsonAsync("Usuario/Registrar", request);
        return await LeerResultadoAsync<UsuarioApiResponse>(response);
    }

    public async Task<ApiResultado<LoginApiResponse>> IniciarSesionAsync(LoginViewModel model)
    {
        var request = new LoginApiRequest(model.Email, model.Password);
        using var response = await _httpClient.PostAsJsonAsync("Login", request);
        return await LeerResultadoAsync<LoginApiResponse>(response);
    }

    public async Task<ApiResultado<UsuarioApiResponse>> ActualizarAsync(int id, UserAdminViewModel model)
    {
        var request = new ActualizarUsuarioApiRequest(
            model.Email,
            model.FullName,
            ApiRolMapper.ObtenerTipoUsuario(model.Role),
            model.IsActive);

        using var response = await _httpClient.PutAsJsonAsync($"Usuario/{id}", request);
        return await LeerResultadoAsync<UsuarioApiResponse>(response);
    }

    public async Task<ApiResultado<UsuarioApiResponse>> CambiarEstadoAsync(int id, bool activar)
    {
        var endpoint = activar ? $"Usuario/{id}/activar" : $"Usuario/{id}/desactivar";
        using var response = await _httpClient.PatchAsync(endpoint, null);
        return await LeerResultadoAsync<UsuarioApiResponse>(response);
    }

    private static async Task<ApiResultado<T>> LeerResultadoAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>();
            return ApiResultado<T>.Correcto(data!);
        }

        var mensaje = await LeerMensajeErrorAsync(response);
        return ApiResultado<T>.Error(mensaje, response.StatusCode);
    }

    private static async Task<string> LeerMensajeErrorAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return "Usuario o contraseña incorrectos.";
        }

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return "No se pudo completar la solicitud con la API.";
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? "No se pudo completar la solicitud con la API.";
            }
        }
        catch
        {
            // Si la API devuelve texto plano, se usa el contenido recibido.
        }

        return content.Trim('"');
    }
}
