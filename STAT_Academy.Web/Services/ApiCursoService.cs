using STAT_Academy.Web.Models.Cursos;
using System.Net.Http.Json;

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
    }
}