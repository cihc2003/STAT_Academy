using STAT_Academy.Web.Models.Blog;
using System.Net.Http.Json;
using System.Text.Json;

namespace STAT_Academy.Web.Services
{
    public class ApiBlogService
    {
        private readonly HttpClient _httpClient;

        public ApiBlogService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<BlogResponse>> ObtenerBlogs()
        {
            var response = await _httpClient.GetAsync("api/Blog");

            if (!response.IsSuccessStatusCode)
            {
                return new List<BlogResponse>();
            }

            var contenido = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(contenido))
            {
                return new List<BlogResponse>();
            }

            return JsonSerializer.Deserialize<List<BlogResponse>>(
                       contenido,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       }) ?? new List<BlogResponse>();
        }

        public async Task<BlogResponse?> ObtenerBlogPorId(int id)
        {
            var response = await _httpClient.GetAsync($"api/Blog/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var contenido = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(contenido))
            {
                return null;
            }

            return JsonSerializer.Deserialize<BlogResponse>(
                contenido,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        public async Task<(bool exitoso, string mensaje)> CrearBlog(CreateBlogViewModel model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/Blog", model);
            return await ProcesarRespuesta(response);
        }

        public async Task<(bool exitoso, string mensaje)> ActualizarBlog(int id, UpdateBlogViewModel model)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/Blog/{id}", model);
            return await ProcesarRespuesta(response);
        }

        public async Task<(bool exitoso, string mensaje)> DesactivarBlog(int id)
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, $"api/Blog/{id}/desactivar");
            var response = await _httpClient.SendAsync(request);
            return await ProcesarRespuesta(response);
        }

        private static async Task<(bool exitoso, string mensaje)> ProcesarRespuesta(HttpResponseMessage response)
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

                if (documento.RootElement.TryGetProperty("message", out var mensajeElement))
                {
                    return (
                        response.IsSuccessStatusCode,
                        mensajeElement.GetString() ?? "Operación realizada correctamente."
                    );
                }
            }
            catch (JsonException)
            {
            }

            return (
                response.IsSuccessStatusCode,
                response.IsSuccessStatusCode
                    ? "Operación realizada correctamente."
                    : "No se pudo procesar la solicitud."
            );
        }
    }
}