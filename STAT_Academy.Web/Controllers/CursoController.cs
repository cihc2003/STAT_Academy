using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Models;
using System.Text.Json;

namespace STAT_Academy.Web.Controllers
{
    public class CursoController : Controller
    {
        private readonly HttpClient _http;

        public CursoController(HttpClient http)
        {
            _http = http;
        }

        public async Task<IActionResult> Index()
        {
            var response = await _http.GetAsync("https://localhost:7163/api/Curso");

            if (!response.IsSuccessStatusCode)
                return View(new List<CursoModel>());

            var json = await response.Content.ReadAsStringAsync();

            var cursos = JsonSerializer.Deserialize<List<CursoModel>>(json,
                new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });

            return View(cursos);
        }

        public async Task<IActionResult> Details(int id)
        {
            var response = await _http.GetAsync($"https://localhost:7163/api/Curso/{id}");

            if (!response.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var json = await response.Content.ReadAsStringAsync();

            var curso = JsonSerializer.Deserialize<CursoModel>(json,
                new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });

            return View(curso);
        }

        public async Task<IActionResult> MisCursos()
        {
            int usuario = Convert.ToInt32(HttpContext.Session.GetString("UsuarioId"));

            var response = await _http.GetAsync($"https://localhost:7163/api/EstudianteCurso/{usuario}");

            if (!response.IsSuccessStatusCode)
                return View(new List<CursoModel>());

            var json = await response.Content.ReadAsStringAsync();

            var cursos = JsonSerializer.Deserialize<List<CursoModel>>(json,
                new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });

            return View(cursos);
        }

        public async Task<IActionResult> Inscribirse(int idCurso)
        {
            int usuario = Convert.ToInt32(HttpContext.Session.GetString("UsuarioId"));

            var response = await _http.PostAsync($"https://localhost:7163/api/EstudianteCurso/{usuario}/{idCurso}", null);

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Curso inscrito correctamente";

            else
                TempData["Error"] = "No fue posible realizar la inscripción";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Tareas(int idCurso)
        {
            var response = await _http.GetAsync($"https://localhost:7163/api/Tarea/curso/{idCurso}");

            if (!response.IsSuccessStatusCode)
                return View(new List<TareaModel>());

            var json = await response.Content.ReadAsStringAsync();

            var tareas = JsonSerializer.Deserialize<List<TareaModel>>(json,
                new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });

            return View(tareas);
        }
    }
}