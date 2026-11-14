using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Models.Tutor;
using STAT_Academy.Web.Services;

namespace STAT_Academy.Web.Controllers
{
    /// <summary>
    /// Panel del tutor. La API decide qué cursos puede ver cada tutor (solo los asignados);
    /// si pide uno ajeno, la API responde 403 y aquí se muestra "no encontrado".
    /// </summary>
    [Authorize(Roles = "Tutor,Admin")]
    public class TutorController : Controller
    {
        private readonly ApiTutorService _api;

        public TutorController(ApiTutorService api)
        {
            _api = api;
        }

        // ------------------------------------------------------------------
        // Cursos asignados
        // ------------------------------------------------------------------

        public async Task<IActionResult> Index()
        {
            return View(await _api.Cursos() ?? []);
        }

        // Compatibilidad con el enlace antiguo del menú (/Tutor/Curso sin id).
        public async Task<IActionResult> Curso(int? id)
        {
            if (id == null)
                return RedirectToAction(nameof(Index));

            var curso = await _api.Curso(id.Value);
            if (curso == null)
                return NotFound();

            return View(new TutorCursoPagina
            {
                Curso = curso,
                Tareas = await _api.Tareas(id.Value) ?? [],
                Material = await _api.Material(id.Value) ?? [],
                NuevoMaterial = new MaterialFormViewModel { CursoId = id.Value }
            });
        }

        public async Task<IActionResult> Estudiantes(int id)
        {
            var curso = await _api.Curso(id);
            if (curso == null)
                return NotFound();

            return View(new TutorEstudiantesPagina
            {
                Curso = curso,
                Estudiantes = await _api.Estudiantes(id) ?? []
            });
        }

        public async Task<IActionResult> Calificaciones(int id)
        {
            var curso = await _api.Curso(id);
            if (curso == null)
                return NotFound();

            return View(new TutorCalificacionesPagina
            {
                Curso = curso,
                Calificaciones = await _api.Calificaciones(id) ?? new()
            });
        }

        // ------------------------------------------------------------------
        // Tareas
        // ------------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> NuevaTarea(int id)
        {
            var curso = await _api.Curso(id);
            if (curso == null)
                return NotFound();

            // El "id" de esta URL es el del CURSO. Sin esto, ASP.NET lo reutiliza para el campo
            // oculto Id del formulario y la tarea nueva se trataría como "editar la tarea {id}".
            ModelState.Clear();

            return View("TareaForm", new TareaFormViewModel
            {
                CursoId = id,
                CursoNombre = curso.Nombre,
                // Por defecto la tarea abre hoy (o al inicio del curso si aún no empieza).
                FechaInicio = curso.FechaInicio > DateTime.Today ? curso.FechaInicio : DateTime.Today
            });
        }

        [HttpGet]
        public async Task<IActionResult> EditarTarea(int id)
        {
            var entregas = await _api.Entregas(id);
            if (entregas == null)
                return NotFound();

            var t = entregas.Tarea;

            return View("TareaForm", new TareaFormViewModel
            {
                Id = t.Id,
                CursoId = t.CursoId,
                CursoNombre = entregas.Curso,
                Titulo = t.Titulo,
                Descripcion = t.Descripcion,
                FechaInicio = t.FechaInicio ?? DateTime.Today,
                FechaLimite = t.FechaLimite ?? DateTime.Today.AddDays(7)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarTarea(TareaFormViewModel model)
        {
            if (model.FechaLimite < model.FechaInicio)
                ModelState.AddModelError(nameof(model.FechaLimite), "La fecha límite no puede ser anterior a la fecha de inicio.");

            if (!ModelState.IsValid)
                return View("TareaForm", model);

            var (exitoso, mensaje) = model.Id == null
                ? await _api.CrearTarea(model)
                : await _api.EditarTarea(model);

            if (!exitoso)
            {
                ModelState.AddModelError(string.Empty, mensaje);
                return View("TareaForm", model);
            }

            TempData["Success"] = model.Id == null ? "Tarea creada." : "Tarea actualizada.";
            return RedirectToAction(nameof(Curso), new { id = model.CursoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EstadoTarea(int id, int cursoId, bool activa)
        {
            var (exitoso, mensaje) = await _api.CambiarEstadoTarea(id, activa);
            TempData[exitoso ? "Success" : "Error"] = mensaje;

            return RedirectToAction(nameof(Curso), new { id = cursoId });
        }

        // ------------------------------------------------------------------
        // Entregas y calificación
        // ------------------------------------------------------------------

        public async Task<IActionResult> Entregas(int id)
        {
            var entregas = await _api.Entregas(id);
            return entregas == null ? NotFound() : View(entregas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calificar(CalificarViewModel model)
        {
            if (model.CalificacionNumero is not decimal nota || nota < 0 || nota > 100)
                ModelState.AddModelError(nameof(model.Calificacion), "La calificación debe ser un número entre 0 y 100.");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                                    ?? "Revise la calificación.";
                return RedirectToAction(nameof(Entregas), new { id = model.TareaId });
            }

            var (exitoso, mensaje) = await _api.Calificar(model);
            TempData[exitoso ? "Success" : "Error"] = mensaje;

            return RedirectToAction(nameof(Entregas), new { id = model.TareaId });
        }

        public async Task<IActionResult> ArchivoEntrega(int id, CancellationToken ct) =>
            await Reenviar(await _api.DescargarArchivoEntrega(id, ct), ct);

        // ------------------------------------------------------------------
        // Material
        // ------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaterialFormViewModel.MaxBytesPeticion)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaterialFormViewModel.MaxBytesPeticion)]
        public async Task<IActionResult> AgregarMaterial([Bind(Prefix = "NuevoMaterial")] MaterialFormViewModel model)
        {
            var tieneArchivo = model.Archivo is { Length: > 0 };
            var tieneEnlace = !string.IsNullOrWhiteSpace(model.Enlace);

            if (tieneArchivo == tieneEnlace)
                ModelState.AddModelError(string.Empty, "Suba un archivo o indique un enlace (solo uno de los dos).");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage;
                return RedirectToAction(nameof(Curso), new { id = model.CursoId });
            }

            var (exitoso, mensaje) = await _api.AgregarMaterial(model);
            TempData[exitoso ? "Success" : "Error"] = mensaje;

            return RedirectToAction(nameof(Curso), new { id = model.CursoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitarMaterial(int id, int cursoId)
        {
            var (exitoso, mensaje) = await _api.QuitarMaterial(id);
            TempData[exitoso ? "Success" : "Error"] = mensaje;

            return RedirectToAction(nameof(Curso), new { id = cursoId });
        }

        public async Task<IActionResult> Material(int id, CancellationToken ct) =>
            await Reenviar(await _api.DescargarMaterial(id, ct), ct);

        // ------------------------------------------------------------------

        /// <summary>Reenvía al navegador un archivo descargado de la API.</summary>
        private async Task<IActionResult> Reenviar(HttpResponseMessage? respuesta, CancellationToken ct)
        {
            if (respuesta == null)
                return NotFound();

            HttpContext.Response.RegisterForDispose(respuesta);

            var tipo = respuesta.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var nombre = respuesta.Content.Headers.ContentDisposition?.FileNameStar
                         ?? respuesta.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                         ?? "archivo";

            return File(await respuesta.Content.ReadAsStreamAsync(ct), tipo, nombre);
        }
    }
}
