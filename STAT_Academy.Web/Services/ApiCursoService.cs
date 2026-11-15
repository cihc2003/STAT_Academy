using STAT_Academy.Web.Models.Admin;
using STAT_Academy.Web.Models.Cursos;
using System.Net.Http.Headers;
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


        /// <summary>Material de tipo archivo (contenedor privado): la API valida la matrícula.</summary>
        public async Task<HttpResponseMessage?> DescargarMaterial(int materialId, CancellationToken ct)
        {
            var response = await _httpClient.GetAsync($"api/MaterialCurso/{materialId}/descargar", HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.IsSuccessStatusCode)
                return response;

            response.Dispose();
            return null;
        }

        // --- Aula del estudiante (api/Aula) ---------------------------------

        public async Task<AulaCurso?> GetAula(int cursoId)
        {
            using var response = await _httpClient.GetAsync($"api/Aula/cursos/{cursoId}");
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<AulaCurso>(JsonWeb) : null;
        }

        public async Task<AulaTarea?> GetAulaTarea(int tareaId)
        {
            using var response = await _httpClient.GetAsync($"api/Aula/tareas/{tareaId}");
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<AulaTarea>(JsonWeb) : null;
        }

        /// <summary>Entrega o modifica la entrega: archivos nuevos (se suman), enlace o quitar el enlace.</summary>
        public async Task<(bool exitoso, string mensaje)> Entregar(EntregaFormViewModel m)
        {
            using var contenido = new MultipartFormDataContent();

            if (!string.IsNullOrWhiteSpace(m.Enlace))
                contenido.Add(new StringContent(m.Enlace.Trim()), "enlace");

            if (m.QuitarEnlace)
                contenido.Add(new StringContent("true"), "quitarEnlace");

            foreach (var archivo in m.Archivos.Where(a => a is { Length: > 0 }))
            {
                var parte = new StreamContent(archivo.OpenReadStream());
                parte.Headers.ContentType = MediaTypeHeaderValue.TryParse(archivo.ContentType, out var tipo)
                    ? tipo
                    : new MediaTypeHeaderValue("application/octet-stream");
                contenido.Add(parte, "archivos", Path.GetFileName(archivo.FileName));
            }

            using var response = await _httpClient.PostAsync($"api/Aula/tareas/{m.TareaId}/entrega", contenido);
            var (exitoso, mensaje) = await ApiRespuesta.Leer(response);
            if (!exitoso)
                return (false, mensaje);

            var soloQuitaEnlace = m.QuitarEnlace && string.IsNullOrWhiteSpace(m.Enlace) && !m.Archivos.Any(a => a is { Length: > 0 });
            return (true, soloQuitaEnlace ? "Enlace quitado de la entrega." : "Entrega guardada. Tu tutor ya puede revisarla.");
        }

        public async Task<(bool exitoso, string mensaje)> QuitarArchivoEntrega(int archivoId)
        {
            using var response = await _httpClient.DeleteAsync($"api/Aula/entregas/archivos/{archivoId}");
            return await ApiRespuesta.Leer(response);
        }

        public async Task<HttpResponseMessage?> DescargarArchivoEntrega(int archivoId, CancellationToken ct)
        {
            var response = await _httpClient.GetAsync($"api/Aula/entregas/archivos/{archivoId}/descargar", HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.IsSuccessStatusCode)
                return response;

            response.Dispose();
            return null;
        }

        /// <summary>Documento que el tutor adjuntó a una tarea (la API valida la matrícula).</summary>
        public async Task<HttpResponseMessage?> DescargarAdjuntoTarea(int adjuntoId, CancellationToken ct)
        {
            var response = await _httpClient.GetAsync($"api/Aula/adjuntos/{adjuntoId}/descargar", HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.IsSuccessStatusCode)
                return response;

            response.Dispose();
            return null;
        }

        private static readonly JsonSerializerOptions JsonWeb = new() { PropertyNameCaseInsensitive = true };

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

        public async Task<(bool ok, string message)> EditarCursoAsync(int id, CursoFormViewModel model)
        {
            var request = new CursoCreateRequest
            {
                fk_tutor = model.fk_tutor,
                fk_creador = model.fk_creador,
                nombre = model.nombre,
                descripcion = model.descripcion,
                precio = model.precio,
                fechaInicio = model.fechaInicio,
                fechaFin = model.fechaFin
            };

            var response = await _httpClient.PutAsJsonAsync($"api/Curso/{id}", request);
            return await ProcesarRespuesta(response);
        }

        /// <summary>Crea el curso y devuelve su id (necesario para subir la portada después).</summary>
        public async Task<(bool ok, string message, int? id)> CrearCursoAsync(CursoFormViewModel model)
        {
            var request = new CursoCreateRequest
            {
                fk_tutor = model.fk_tutor,
                fk_creador = model.fk_creador,
                nombre = model.nombre,
                descripcion = model.descripcion,
                precio = model.precio,
                fechaInicio = model.fechaInicio,
                fechaFin = model.fechaFin
            };

            using var response = await _httpClient.PostAsJsonAsync("api/Curso", request);
            if (!response.IsSuccessStatusCode)
            {
                var (ok, message) = await ProcesarRespuesta(response);
                return (ok, message, null);
            }

            var creado = await response.Content.ReadFromJsonAsync<CursoAdminResponse>();
            return (true, "Curso creado correctamente.", creado?.id);
        }

        // --- Portada -----------------------------------------------------------

        public async Task<(bool ok, string message)> SubirPortadaAsync(int cursoId, IFormFile archivo)
        {
            using var contenido = new MultipartFormDataContent();

            var parte = new StreamContent(archivo.OpenReadStream());
            parte.Headers.ContentType = MediaTypeHeaderValue.TryParse(archivo.ContentType, out var tipo)
                ? tipo
                : new MediaTypeHeaderValue("application/octet-stream");
            contenido.Add(parte, "imagen", Path.GetFileName(archivo.FileName));

            using var response = await _httpClient.PostAsync($"api/Curso/{cursoId}/portada", contenido);
            var (ok, mensaje) = await ApiRespuesta.Leer(response);
            return (ok, ok ? "Portada actualizada." : mensaje);
        }

        public async Task<(bool ok, string message)> QuitarPortadaAsync(int cursoId)
        {
            using var response = await _httpClient.DeleteAsync($"api/Curso/{cursoId}/portada");
            return await ApiRespuesta.Leer(response);
        }

        // -----------------------------------------------------------------------

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
        public DateTime fechaInicio { get; set; }
        public DateTime fechaFin { get; set; }
    }
}