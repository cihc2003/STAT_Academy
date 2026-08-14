using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Models;
using STAT_Academy.Web.Models.Cursos;
using STAT_Academy.Web.Services;
using System.Security.Claims;
using System.Text.Json;

namespace STAT_Academy.Web.Controllers
{
    public class CursoController : Controller
    {
        private readonly HttpClient _http;
        private readonly ApiCursoService _apiCursos;

        public CursoController(HttpClient http, ApiCursoService apiCursos)
        {
            _http = http;
            _apiCursos = apiCursos;
        }

        // El id del estudiante logueado viaja en la cookie desde el Login.
        private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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

        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> MisCursos()
        {
            var cursos = await _apiCursos.GetMisCursos(UsuarioId) ?? [];

            return View(cursos);
        }

        // Vista general del curso + contenido organizado por semana (tareas y material).
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Contenido(int id)
        {
            var curso = await _apiCursos.GetCurso(id);

            if (curso == null)
                return NotFound();

            var tareas = await _apiCursos.GetTareasPorCurso(id) ?? [];
            var material = await _apiCursos.GetMaterialPorCurso(id) ?? [];

            var model = new CursoContenidoViewModel
            {
                Curso = curso,
                Semanas = Enumerable.Range(1, Math.Max(curso.duracionSemanas, 1))
                    .Select(numero => new SemanaViewModel
                    {
                        Numero = numero,
                        Tareas = tareas.Where(t => t.Semana == numero).ToList(),
                        Material = material.Where(m => m.Semana == numero).ToList()
                    })
                    .ToList(),
                // Tareas/material sin semana asignada, para no perderlos de la vista.
                SinSemana = new SemanaViewModel
                {
                    Numero = 0,
                    Tareas = tareas.Where(t => t.Semana == null).ToList(),
                    Material = material.Where(m => m.Semana == null).ToList()
                }
            };

            return View(model);
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