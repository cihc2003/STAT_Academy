using STAT_Academy.Web.Models.Facturas;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace STAT_Academy.Web.Services
{
    /// <summary>Cliente de api/Factura y de la compra de cursos. El token lo agrega BearerTokenHandler.</summary>
    public class ApiFacturaService
    {
        private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _http;

        public ApiFacturaService(HttpClient http)
        {
            _http = http;
        }

        // --- Usuario -------------------------------------------------------

        public Task<List<FacturaResumen>?> MisFacturas() => Get<List<FacturaResumen>>("api/Factura/mias");

        public Task<FacturaDetalle?> Detalle(int facturaId) => Get<FacturaDetalle>($"api/Factura/{facturaId}");

        public async Task<(bool exitoso, string mensaje)> EnviarComprobante(EnviarComprobanteViewModel m)
        {
            using var contenido = new MultipartFormDataContent
            {
                { new StringContent(m.Metodo ?? ""), "metodo" }
            };

            if (!string.IsNullOrWhiteSpace(m.Referencia))
                contenido.Add(new StringContent(m.Referencia.Trim()), "referencia");

            if (m.Archivo is { Length: > 0 } archivo)
            {
                var parte = new StreamContent(archivo.OpenReadStream());
                parte.Headers.ContentType = MediaTypeHeaderValue.TryParse(archivo.ContentType, out var tipo)
                    ? tipo
                    : new MediaTypeHeaderValue("application/octet-stream");
                contenido.Add(parte, "archivo", Path.GetFileName(archivo.FileName));
            }

            using var response = await _http.PostAsync($"api/Factura/{m.FacturaId}/comprobantes", contenido);
            return await ApiRespuesta.Leer(response);
        }

        /// <summary>Compra un curso de pago. Devuelve la factura creada o la pendiente que ya existía.</summary>
        public async Task<(int? facturaId, string mensaje)> ComprarCurso(int cursoId)
        {
            using var response = await _http.PostAsync($"api/Compra/curso/{cursoId}", null);
            var cuerpo = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<CompraCursoRespuesta>(Json)
                : null;

            if (cuerpo?.facturaId is int id)
                return (id, cuerpo.message ?? "");

            var (_, mensaje) = await ApiRespuesta.Leer(response);
            return (null, mensaje);
        }

        private sealed class CompraCursoRespuesta
        {
            public int? facturaId { get; set; }
            public string? message { get; set; }
        }

        /// <summary>Archivo del comprobante (la API valida que sea del usuario o que sea Admin).</summary>
        public async Task<HttpResponseMessage?> DescargarComprobante(int pagoId, CancellationToken ct)
        {
            var response = await _http.GetAsync($"api/Factura/pagos/{pagoId}/comprobante", HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.IsSuccessStatusCode)
                return response;

            response.Dispose();
            return null;
        }

        // --- Administración ------------------------------------------------

        public Task<List<FacturaResumen>?> TodasLasFacturas() => Get<List<FacturaResumen>>("api/Factura/admin");

        public Task<List<PagoComprobante>?> Pagos(string? estado) =>
            Get<List<PagoComprobante>>(string.IsNullOrWhiteSpace(estado)
                ? "api/Factura/admin/pagos"
                : $"api/Factura/admin/pagos?estado={Uri.EscapeDataString(estado)}");

        public async Task<int> PagosPendientes() =>
            await Get<int?>("api/Factura/admin/pagos/pendientes/cantidad") ?? 0;

        public async Task<(bool exitoso, string mensaje)> Aprobar(int pagoId)
        {
            using var response = await _http.PutAsync($"api/Factura/admin/pagos/{pagoId}/aprobar", null);
            return await ApiRespuesta.Leer(response);
        }

        public async Task<(bool exitoso, string mensaje)> Rechazar(int pagoId, string observacion)
        {
            using var response = await _http.PutAsJsonAsync($"api/Factura/admin/pagos/{pagoId}/rechazar", new { observacion });
            return await ApiRespuesta.Leer(response);
        }

        // --- Ayudantes -----------------------------------------------------

        private async Task<T?> Get<T>(string url)
        {
            using var response = await _http.GetAsync(url);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<T>(Json)
                : default;
        }
    }
}
