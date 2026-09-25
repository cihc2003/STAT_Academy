using STAT_Academy.Web.Models.Admin;
using STAT_Academy.Web.Models.Cursos;
using System.Net.Http.Json;
using System.Text.Json;

namespace STAT_Academy.Web.Services
{
    public class ApiCursoService
    {
        private readonly HttpClient _httpClient;

        public ApiCursoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<CursoResponse?> GetCurso(int id)
        {
            return await _httpClient.GetFromJsonAsync<CursoResponse>($"api/Curso/{id}");
        }

        public async Task<List<CursoCompradoResponse>?> GetMisCursos(int estudianteId)
        {
            return await _httpClient.GetFromJsonAsync<List<CursoCompradoResponse>>($"api/EstudianteCurso/{estudianteId}");
        }

        public async Task<List<TareaResponse>?> GetTareasPorCurso(int cursoId)
        {
            return await _httpClient.GetFromJsonAsync<List<TareaResponse>>($"api/Tarea/curso/{cursoId}");
        }

        public async Task<List<MaterialResponse>?> GetMaterialPorCurso(int cursoId)
        {
            return await _httpClient.GetFromJsonAsync<List<MaterialResponse>>($"api/MaterialCurso/curso/{cursoId}");
        }


        public async Task<List<CursoAdminResponse>> GetCursosAsync()
        {
            var response = await _httpClient.GetAsync("api/Curso");

            if (!response.IsSuccessStatusCode)
                return [];

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<CursoAdminResponse>>(
                       json,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       }) ?? [];
        }

        public async Task<CursoAdminResponse?> GetCursoByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/Curso/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<CursoAdminResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        public async Task<(bool ok, string message)> CrearCursoAsync(CursoFormViewModel model)
        {
            var request = new CursoCreateRequest
            {
                fk_tutor = model.fk_tutor,
                fk_creador = model.fk_creador,
                nombre = model.nombre,
                descripcion = model.descripcion,
                precio = model.precio,
                duracionSemanas = model.duracionSemanas,
                fechaInicio = model.fechaInicio,
                fechaFin = model.fechaFin
            };

            var response = await _httpClient.PostAsJsonAsync("api/Curso", request);
            return await ProcesarRespuesta(response);
        }

        public async Task<(bool ok, string message)> EditarCursoAsync(int id, CursoFormViewModel model)
        {
            var request = new CursoCreateRequest
            {
                fk_tutor = model.fk_tutor,
                fk_creador = model.fk_creador,
                nombre = model.nombre,
                descripcion = model.descripcion,
                precio = model.precio,
                duracionSemanas = model.duracionSemanas,
                fechaInicio = model.fechaInicio,
                fechaFin = model.fechaFin
            };

            var response = await _httpClient.PutAsJsonAsync($"api/Curso/{id}", request);
            return await ProcesarRespuesta(response);
        }

        public async Task<(bool ok, string message)> ActivarCursoAsync(int id)
        {
            var response = await _httpClient.PatchAsync($"api/Curso/activar/{id}", null);
            return await ProcesarRespuesta(response);
        }

        public async Task<(bool ok, string message)> DesactivarCursoAsync(int id)
        {
            var response = await _httpClient.PatchAsync($"api/Curso/desactivar/{id}", null);
            return await ProcesarRespuesta(response);
        }

        private static async Task<(bool ok, string message)> ProcesarRespuesta(HttpResponseMessage response)
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

                if (documento.RootElement.TryGetProperty("message", out var messageElement))
                {
                    return (
                        response.IsSuccessStatusCode,
                        messageElement.GetString() ?? "Operación realizada correctamente."
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

    public class CursoCreateRequest
    {
        public int fk_tutor { get; set; }
        public int fk_creador { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string descripcion { get; set; } = string.Empty;
        public decimal precio { get; set; }
        public int duracionSemanas { get; set; }
        public DateTime fechaInicio { get; set; }
        public DateTime fechaFin { get; set; }
    }
}