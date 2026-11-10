using STAT_Academy.Web.Models.Blog;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace STAT_Academy.Web.Services
{
    public class ApiBlogService
    {
        private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _httpClient;

        public ApiBlogService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<BlogResponse>> ObtenerBlogs()
        {
            var response = await _httpClient.GetAsync("api/Blog");

            if (!response.IsSuccessStatusCode)
                return [];

            return await response.Content.ReadFromJsonAsync<List<BlogResponse>>(Json) ?? [];
        }

        public async Task<BlogResponse?> ObtenerBlogPorId(int id)
        {
            var response = await _httpClient.GetAsync($"api/Blog/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<BlogResponse>(Json);
        }

        /// <returns>Si se creó, el id de la nueva entrada.</returns>
        public async Task<(bool exitoso, string mensaje, int? id)> CrearBlog(CreateBlogViewModel model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/Blog", new
            {
                titulo = model.Titulo,
                contenido = model.Contenido
            });

            if (!response.IsSuccessStatusCode)
            {
                var (_, mensaje) = await ProcesarRespuesta(response);
                return (false, mensaje, null);
            }

            var creado = await response.Content.ReadFromJsonAsync<BlogResponse>(Json);
            return (true, "La entrada fue publicada.", creado?.Id);
        }

        public async Task<(bool exitoso, string mensaje)> ActualizarBlog(int id, UpdateBlogViewModel model)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/Blog/{id}", model);
            return await ProcesarRespuesta(response);
        }

        public Task<(bool exitoso, string mensaje)> DesactivarBlog(int id) =>
            Enviar(HttpMethod.Patch, $"api/Blog/{id}/desactivar");

        public Task<(bool exitoso, string mensaje)> ActivarBlog(int id) =>
            Enviar(HttpMethod.Patch, $"api/Blog/{id}/activar");

        public async Task<(bool exitoso, string mensaje)> SubirArchivos(int id, IEnumerable<IFormFile> archivos)
        {
            using var contenido = new MultipartFormDataContent();

            foreach (var archivo in archivos.Where(a => a.Length > 0))
            {
                var parte = new StreamContent(archivo.OpenReadStream());
                parte.Headers.ContentType = MediaTypeHeaderValue.TryParse(archivo.ContentType, out var tipo)
                    ? tipo
                    : new MediaTypeHeaderValue("application/octet-stream");

                contenido.Add(parte, "archivos", Path.GetFileName(archivo.FileName));
            }

            var response = await _httpClient.PostAsync($"api/Blog/{id}/archivos", contenido);

            if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge)
                return (false, "Los archivos superan el tamaño máximo permitido.");

            var (exitoso, mensaje) = await ProcesarRespuesta(response);
            return (exitoso, exitoso ? "Archivos agregados." : mensaje);
        }

        /// <summary>
        /// Abre un documento adjunto (contenedor privado) a través de la API.
        /// Quien llama debe liberar la respuesta (la Web la reenvía como stream).
        /// </summary>
        public async Task<HttpResponseMessage?> DescargarDocumento(int id, int archivoId, CancellationToken ct)
        {
            var response = await _httpClient.GetAsync(
                $"api/Blog/{id}/archivos/{archivoId}/descargar",
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            if (response.IsSuccessStatusCode)
                return response;

            response.Dispose();
            return null;
        }

        public Task<(bool exitoso, string mensaje)> QuitarArchivo(int id, int archivoId) =>
            Enviar(HttpMethod.Delete, $"api/Blog/{id}/archivos/{archivoId}");

        public Task<(bool exitoso, string mensaje)> MarcarPrincipal(int id, int archivoId) =>
            Enviar(HttpMethod.Patch, $"api/Blog/{id}/archivos/{archivoId}/principal");

        private async Task<(bool exitoso, string mensaje)> Enviar(HttpMethod metodo, string url)
        {
            var response = await _httpClient.SendAsync(new HttpRequestMessage(metodo, url));
            return await ProcesarRespuesta(response);
        }

        private static async Task<(bool exitoso, string mensaje)> ProcesarRespuesta(HttpResponseMessage response)
        {
            var exitoso = response.IsSuccessStatusCode;
            var porDefecto = exitoso
                ? "Operación realizada correctamente."
                : response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "Su sesión expiró. Inicie sesión nuevamente.",
                    HttpStatusCode.Forbidden => "No tiene permiso para modificar esta entrada.",
                    HttpStatusCode.NotFound => "La entrada no existe.",
                    _ => "No se pudo procesar la solicitud."
                };

            var contenido = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(contenido))
                return (exitoso, porDefecto);

            try
            {
                using var documento = JsonDocument.Parse(contenido);
                var raiz = documento.RootElement;

                if (raiz.ValueKind == JsonValueKind.Object)
                {
                    if (raiz.TryGetProperty("message", out var mensaje) && mensaje.ValueKind == JsonValueKind.String)
                        return (exitoso, mensaje.GetString() ?? porDefecto);

                    // Errores de validación automáticos de la API (ProblemDetails)
                    if (raiz.TryGetProperty("errors", out var errores) && errores.ValueKind == JsonValueKind.Object)
                    {
                        var primero = errores.EnumerateObject()
                            .SelectMany(e => e.Value.ValueKind == JsonValueKind.Array ? e.Value.EnumerateArray() : Enumerable.Empty<JsonElement>())
                            .Select(e => e.GetString())
                            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));

                        if (primero != null)
                            return (exitoso, primero);
                    }
                }
            }
            catch (JsonException)
            {
            }

            return (exitoso, porDefecto);
        }
    }
}
