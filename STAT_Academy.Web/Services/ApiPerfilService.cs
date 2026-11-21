using STAT_Academy.Web.Models.Perfil;
using System.Net.Http.Json;
using System.Text.Json;

namespace STAT_Academy.Web.Services
{
    /// <summary>Cliente de api/Perfil: la API identifica al usuario por el token (no se envían ids).</summary>
    public class ApiPerfilService
    {
        private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _http;

        public ApiPerfilService(HttpClient http)
        {
            _http = http;
        }

        public async Task<PerfilDatos?> Obtener()
        {
            using var response = await _http.GetAsync("api/Perfil");
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<PerfilDatos>(Json)
                : null;
        }

        /// <summary>Cambia el nombre propio. Si tuvo éxito devuelve el perfil actualizado.</summary>
        public async Task<(PerfilDatos? perfil, string mensaje)> ActualizarNombre(string nombre)
        {
            using var response = await _http.PutAsJsonAsync("api/Perfil", new { nombre });

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<PerfilDatos>(Json), "Sus datos se guardaron correctamente.");

            var (_, mensaje) = await ApiRespuesta.Leer(response);
            return (null, mensaje);
        }
    }
}
