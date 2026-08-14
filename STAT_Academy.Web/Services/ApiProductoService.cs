using STAT_Academy.Web.Models.Productos;
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

        public async Task<List<ProductoResponse>?> GetProductos()
        {
            return await _httpClient.GetFromJsonAsync<List<ProductoResponse>>("api/Producto");
        }

        public async Task<ProductoResponse?> GetProductoById(int id)
        {
            var productos = await GetProductos();
            return productos?.FirstOrDefault(p => p.id == id);
        }

        public async Task<ProductoResponse?> CrearProducto(ProductoCreateRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/Producto", request);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ProductoResponse>();
        }

        public async Task<ProductoResponse?> EditarProducto(int id, ProductoCreateRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/Producto/{id}", request);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ProductoResponse>();
        }

        public async Task<ProductoResponse?> DesactivarProducto(int id)
        {
            var response = await _httpClient.PutAsync($"api/Producto/desactivar/{id}", null);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ProductoResponse>();
        }

        public async Task<ProductoResponse?> ActivarProducto(int id)
        {
            var response = await _httpClient.PutAsync($"api/Producto/activar/{id}", null);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ProductoResponse>();
        }

        public async Task<List<ProveedorResponse>?> GetProveedores()
        {
            return await _httpClient.GetFromJsonAsync<List<ProveedorResponse>>("api/Proveedor");
        }
    }
}
