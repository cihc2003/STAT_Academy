using STAT_Academy.Web.Models.Tutor;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace STAT_Academy.Web.Services
{
    /// <summary>Cliente de api/Tutor/*. El token del usuario lo agrega BearerTokenHandler.</summary>
    public class ApiTutorService
    {
        private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _http;

        public ApiTutorService(HttpClient http)
        {
            _http = http;
        }

        // --- Lecturas ------------------------------------------------------

        public Task<List<TutorCurso>?> Cursos() => Get<List<TutorCurso>>("api/Tutor/cursos");
        public Task<TutorCurso?> Curso(int cursoId) => Get<TutorCurso>($"api/Tutor/cursos/{cursoId}");
        public Task<List<TutorTarea>?> Tareas(int cursoId) => Get<List<TutorTarea>>($"api/Tutor/cursos/{cursoId}/tareas");
        public Task<List<TutorMaterial>?> Material(int cursoId) => Get<List<TutorMaterial>>($"api/Tutor/cursos/{cursoId}/material");
        public Task<List<TutorEstudiante>?> Estudiantes(int cursoId) => Get<List<TutorEstudiante>>($"api/Tutor/cursos/{cursoId}/estudiantes");
        public Task<TutorCalificaciones?> Calificaciones(int cursoId) => Get<TutorCalificaciones>($"api/Tutor/cursos/{cursoId}/calificaciones");
        public Task<TutorEntregas?> Entregas(int tareaId) => Get<TutorEntregas>($"api/Tutor/tareas/{tareaId}/entregas");

        // --- Tareas --------------------------------------------------------

        public Task<(bool exitoso, string mensaje)> CrearTarea(TareaFormViewModel m) =>
            Enviar(_http.PostAsJsonAsync($"api/Tutor/cursos/{m.CursoId}/tareas", CuerpoTarea(m)));

        public Task<(bool exitoso, string mensaje)> EditarTarea(TareaFormViewModel m) =>
            Enviar(_http.PutAsJsonAsync($"api/Tutor/tareas/{m.Id}", CuerpoTarea(m)));

        public Task<(bool exitoso, string mensaje)> CambiarEstadoTarea(int tareaId, bool activa) =>
            Enviar(_http.PatchAsync($"api/Tutor/tareas/{tareaId}/{(activa ? "activar" : "desactivar")}", null));

        private static object CuerpoTarea(TareaFormViewModel m) => new
        {
            titulo = m.Titulo,
            descripcion = m.Descripcion ?? "",
            fechaInicio = m.FechaInicio,
            fechaLimite = m.FechaLimite
        };

        // --- Calificación --------------------------------------------------

        public Task<(bool exitoso, string mensaje)> Calificar(CalificarViewModel m) =>
            Enviar(_http.PutAsJsonAsync($"api/Tutor/entregas/{m.EntregaId}/calificacion", new
            {
                calificacion = m.CalificacionNumero,
                feedback = m.Feedback
            }));

        // --- Material ------------------------------------------------------

        public async Task<(bool exitoso, string mensaje)> AgregarMaterial(MaterialFormViewModel m)
        {
            using var contenido = new MultipartFormDataContent
            {
                { new StringContent(m.Titulo), "titulo" }
            };

            if (!string.IsNullOrWhiteSpace(m.Enlace))
                contenido.Add(new StringContent(m.Enlace.Trim()), "enlace");

            if (m.Archivo is { Length: > 0 } archivo)
            {
                var parte = new StreamContent(archivo.OpenReadStream());
                parte.Headers.ContentType = MediaTypeHeaderValue.TryParse(archivo.ContentType, out var tipo)
                    ? tipo
                    : new MediaTypeHeaderValue("application/octet-stream");
                contenido.Add(parte, "archivo", Path.GetFileName(archivo.FileName));
            }

            var (exitoso, mensaje) = await Enviar(_http.PostAsync($"api/Tutor/cursos/{m.CursoId}/material", contenido));
            return (exitoso, exitoso ? "Material agregado al curso." : mensaje);
        }

        public Task<(bool exitoso, string mensaje)> QuitarMaterial(int materialId) =>
            Enviar(_http.DeleteAsync($"api/Tutor/material/{materialId}"));

        // --- Descargas (se reenvían tal cual al navegador) ------------------

        public Task<HttpResponseMessage?> DescargarArchivoEntrega(int archivoId, CancellationToken ct) =>
            Descargar($"api/Tutor/entregas/archivos/{archivoId}/descargar", ct);

        public Task<HttpResponseMessage?> DescargarMaterial(int materialId, CancellationToken ct) =>
            Descargar($"api/MaterialCurso/{materialId}/descargar", ct);

        // --- Ayudantes -----------------------------------------------------

        private async Task<T?> Get<T>(string url)
        {
            using var response = await _http.GetAsync(url);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<T>(Json)
                : default;
        }

        private static async Task<(bool exitoso, string mensaje)> Enviar(Task<HttpResponseMessage> peticion)
        {
            using var response = await peticion;
            return await ApiRespuesta.Leer(response);
        }

        private async Task<HttpResponseMessage?> Descargar(string url, CancellationToken ct)
        {
            var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.IsSuccessStatusCode)
                return response;

            response.Dispose();
            return null;
        }
    }
}
