using STAT_Academy.Web.Models;
using System.Text;
using System.Text.Json;

namespace STAT_Academy.Web.Services
{
    public class CursoService
    {
        private readonly HttpClient _http;

        public CursoService(HttpClient http)
        {
            _http = http;
        }


        public async Task<List<CursoModel>> ObtenerCursos()
        {
            var response = await _http.GetAsync("Curso");

            if (!response.IsSuccessStatusCode)
                return new List<CursoModel>();

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<CursoModel>>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }


        public async Task<CursoModel> ObtenerCurso(int id)
        {
            var response = await _http.GetAsync($"Curso/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<CursoModel>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }


        public async Task<bool> CrearCurso(CursoModel curso)
        {
            var json = JsonSerializer.Serialize(curso);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");


            var response = await _http.PostAsync("Curso", content);

            return response.IsSuccessStatusCode;
        }


        public async Task<bool> EditarCurso(CursoModel curso)
        {
            var json = JsonSerializer.Serialize(curso);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");


            var response = await _http.PutAsync(
                $"Curso/{curso.Id}",
                content);


            return response.IsSuccessStatusCode;
        }


        public async Task<bool> EliminarCurso(int id)
        {
            var response = await _http.DeleteAsync($"Curso/{id}");

            return response.IsSuccessStatusCode;
        }
    }
}