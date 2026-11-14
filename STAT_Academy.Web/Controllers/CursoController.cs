using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Helpers;
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

        public CursoController(IHttpClientFactory httpClientFactory, ApiCursoService apiCursos)
        {
            _http = httpClientFactory.CreateClient(ApiClient.Nombre);
            _apiCursos = apiCursos;
        }

        private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <param name="buscar">Texto a buscar en el nombre del curso (sin distinguir mayúsculas ni tildes).</param>
        public async Task<IActionResult> Index(string? buscar)
        {
            ViewData["Buscar"] = buscar?.Trim();

            var response = await _http.GetAsync("api/Curso");

            if (!response.IsSuccessStatusCode)
                return View(new List<CursoModel>());

            var json = await response.Content.ReadAsStringAsync();

            var cursos = JsonSerializer.Deserialize<List<CursoModel>>(json,
                new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                }) ?? [];

            // El catálogo es público: solo cursos activos (un Admin recibe también inactivos de la API).
            var visibles = cursos.Where(c => c.Estado).ToList();
            ViewData["Total"] = visibles.Count;

            return View(visibles
                .Where(c => Busqueda.Coincide(c.Nombre, buscar))
                .OrderBy(c => c.Nombre)
                .ToList());
        }

        public async Task<IActionResult> Details(int id)
        {
            using var response = await _http.GetAsync($"api/Curso/{id}");

            // Curso inexistente → página 404; otros errores → aviso en el catálogo.
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return NotFound();

            if (!response.IsSuccessStatusCode)
            {
                var (_, mensaje) = await ApiRespuesta.Leer(response);
                TempData["Error"] = mensaje;
                return RedirectToAction(nameof(Index));
            }

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

        // Aula del curso: datos generales, tareas (por fecha límite) y material.
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Contenido(int id)
        {
            var curso = await _apiCursos.GetCurso(id);

            if (curso == null)
                return NotFound();

            var model = new CursoContenidoViewModel
            {
                Curso = curso,
                Tareas = (await _apiCursos.GetTareasPorCurso(id) ?? [])
                    .OrderBy(t => t.FechaLimite)
                    .ToList(),
                Material = await _apiCursos.GetMaterialPorCurso(id) ?? []
            };

            return View(model);
        }

        /// <summary>Descarga un material del curso (solo matriculados, tutor o Admin; lo valida la API).</summary>
        [Authorize]
        public async Task<IActionResult> Material(int id, CancellationToken ct)
        {
            var respuesta = await _apiCursos.DescargarMaterial(id, ct);
            if (respuesta == null)
                return NotFound();

            HttpContext.Response.RegisterForDispose(respuesta);

            var tipo = respuesta.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var nombre = respuesta.Content.Headers.ContentDisposition?.FileNameStar
                         ?? respuesta.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                         ?? "material";

            return File(await respuesta.Content.ReadAsStreamAsync(ct), tipo, nombre);
        }

        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Inscribirse(int idCurso)
        {
            // La API toma al estudiante del token; no hace falta enviar su id.
            var response = await _http.PostAsync($"api/EstudianteCurso/matricular/{idCurso}", null);

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Curso inscrito correctamente";

            else
                TempData["Error"] = "No fue posible realizar la inscripción";

            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> Tareas(int idCurso)
        {
            var response = await _http.GetAsync($"api/Tarea/curso/{idCurso}");

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