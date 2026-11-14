using STAT_Academy.Web.Models.Carrito;
using System.Net.Http.Json;

namespace STAT_Academy.Web.Services
{
    public class ApiCarritoService
    {
        private readonly HttpClient _httpClient;

        public ApiCarritoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<CarritoResponse?> GetCarrito(int usuarioId)
        {
            return await _httpClient.GetFromJsonAsync<CarritoResponse>($"api/Carrito/usuario/{usuarioId}");
        }

        // Devuelve (éxito, mensajeError, carritoActualizado)
        public async Task<(bool exito, string? error, CarritoResponse? carrito)> Agregar(int usuarioId, int productoId, int cantidad)
        {
            var response = await _httpClient.PostAsJsonAsync("api/Carrito/agregar", new
            {
                usuarioId,
                productoId,
                cantidad
            });

            if (!response.IsSuccessStatusCode)
            {
                var error = await LeerMensajeError(response);
                return (false, error, null);
            }

            var carrito = await response.Content.ReadFromJsonAsync<CarritoResponse>();
            return (true, null, carrito);
        }

        public async Task<(bool exito, string? error, CarritoResponse? carrito)> ActualizarCantidad(int detalleId, int usuarioId, int cantidad)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/Carrito/{detalleId}/usuario/{usuarioId}", new
            {
                cantidad
            });

            if (!response.IsSuccessStatusCode)
            {
                var error = await LeerMensajeError(response);
                return (false, error, null);
            }

            var carrito = await response.Content.ReadFromJsonAsync<CarritoResponse>();
            return (true, null, carrito);
        }

        public async Task<CarritoResponse?> Eliminar(int detalleId, int usuarioId)
        {
            var response = await _httpClient.DeleteAsync($"api/Carrito/{detalleId}/usuario/{usuarioId}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<CarritoResponse>();
        }

        /// <summary>Confirma la compra del carrito: la API crea la factura y descuenta el stock.</summary>
        public async Task<(CompraResponse? compra, string mensaje)> Comprar()
        {
            using var response = await _httpClient.PostAsync("api/Compra", null);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<CompraResponse>(), "");

            var (_, mensaje) = await ApiRespuesta.Leer(response);
            return (null, mensaje);
        }

        public async Task<CompraResponse?> ObtenerCompra(int facturaId)
        {
            using var response = await _httpClient.GetAsync($"api/Compra/{facturaId}");
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<CompraResponse>() : null;
        }

        private static async Task<string> LeerMensajeError(HttpResponseMessage response)
        {
            try
            {
                var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                return body != null && body.TryGetValue("mensaje", out var mensaje)
                    ? mensaje
                    : "No se pudo completar la operación.";
            }
            catch
            {
                return "No se pudo completar la operación.";
            }
        }
    }
}