using STAT_Academy.Web.Models.Productos;
using System.Net.Http.Json;

namespace STAT_Academy.Web.Services
{
    public class ApiProveedorService
    {
        private readonly HttpClient _httpClient;

        public ApiProveedorService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ProveedorResponse>?> GetProveedores()
        {
            return await _httpClient.GetFromJsonAsync<List<ProveedorResponse>>("api/Proveedor");
        }
    }
}
