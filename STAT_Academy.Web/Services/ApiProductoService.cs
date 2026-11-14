using STAT_Academy.Web.Models.Productos;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace STAT_Academy.Web.Services
{
    public class ApiProductoService
    {
        private readonly HttpClient _httpClient;

        public ApiProductoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>Catálogo. La API devuelve solo activos, salvo que el usuario sea Admin.</summary>
        public async Task<List<ProductoResponse>?> GetProductos()
        {
            return await _httpClient.GetFromJsonAsync<List<ProductoResponse>>("api/Producto");
        }

        public async Task<ProductoResponse?> GetProductoById(int id)
        {
            using var response = await _httpClient.GetAsync($"api/Producto/{id}");
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<ProductoResponse>()
                : null;
        }

        public async Task<List<ProductoResponse>> GetStockBajo()
        {
            using var response = await _httpClient.GetAsync("api/Producto/stock-bajo");
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<ProductoResponse>>() ?? []
                : [];
        }

        public Task<(ProductoResponse? producto, string mensaje)> CrearProducto(ProductoCreateRequest request) =>
            Guardar(_httpClient.PostAsJsonAsync("api/Producto", request));

        public Task<(ProductoResponse? producto, string mensaje)> EditarProducto(int id, ProductoCreateRequest request) =>
            Guardar(_httpClient.PutAsJsonAsync($"api/Producto/{id}", request));

        public async Task<ProductoResponse?> DesactivarProducto(int id)
        {
            using var response = await _httpClient.PutAsync($"api/Producto/desactivar/{id}", null);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<ProductoResponse>() : null;
        }

        public async Task<ProductoResponse?> ActivarProducto(int id)
        {
            using var response = await _httpClient.PutAsync($"api/Producto/activar/{id}", null);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<ProductoResponse>() : null;
        }

        public async Task<List<ProveedorResponse>?> GetProveedores()
        {
            return await _httpClient.GetFromJsonAsync<List<ProveedorResponse>>("api/Proveedor");
        }

        // --- Imágenes ----------------------------------------------------------

        public async Task<(bool exitoso, string mensaje)> SubirImagenes(int productoId, IEnumerable<IFormFile> archivos)
        {
            using var contenido = new MultipartFormDataContent();

            foreach (var archivo in archivos.Where(a => a.Length > 0))
            {
                var parte = new StreamContent(archivo.OpenReadStream());
                parte.Headers.ContentType = MediaTypeHeaderValue.TryParse(archivo.ContentType, out var tipo)
                    ? tipo
                    : new MediaTypeHeaderValue("application/octet-stream");
                contenido.Add(parte, "imagenes", Path.GetFileName(archivo.FileName));
            }

            using var response = await _httpClient.PostAsync($"api/Producto/{productoId}/imagenes", contenido);
            var (exitoso, mensaje) = await ApiRespuesta.Leer(response);
            return (exitoso, exitoso ? "Imágenes agregadas." : mensaje);
        }

        public async Task<(bool exitoso, string mensaje)> MarcarPrincipal(int productoId, int imagenId)
        {
            using var response = await _httpClient.PatchAsync($"api/Producto/{productoId}/imagenes/{imagenId}/principal", null);
            return await ApiRespuesta.Leer(response);
        }

        public async Task<(bool exitoso, string mensaje)> QuitarImagen(int productoId, int imagenId)
        {
            using var response = await _httpClient.DeleteAsync($"api/Producto/{productoId}/imagenes/{imagenId}");
            return await ApiRespuesta.Leer(response);
        }

        // -----------------------------------------------------------------------

        private static async Task<(ProductoResponse? producto, string mensaje)> Guardar(Task<HttpResponseMessage> peticion)
        {
            using var response = await peticion;

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<ProductoResponse>(), "");

            var (_, mensaje) = await ApiRespuesta.Leer(response);
            return (null, mensaje);
        }
    }
}
