using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    /// <summary>
    /// Aula del estudiante: sus tareas, sus entregas y sus calificaciones.
    /// El estudiante sale SIEMPRE del token; solo con matrícula activa en un curso activo.
    /// </summary>
    [Authorize(Roles = Roles.Cliente)]
    [Route("api/[controller]")]
    public class AulaController : ApiControllerBase
    {
        private readonly AulaService _service;
        private readonly TutorService _tutor;
        private readonly TareaAdjuntoService _adjuntos;

        public AulaController(AulaService service, TutorService tutor, TareaAdjuntoService adjuntos)
        {
            _service = service;
            _tutor = tutor;
            _adjuntos = adjuntos;
        }

        /// <summary>Tareas del curso con el estado de la entrega propia, progreso y nota final.</summary>
        [HttpGet("cursos/{cursoId}")]
        public async Task<IActionResult> Curso(int cursoId) =>
            await Verificar(cursoId)
            ?? Ok(await _service.CursoAsync(cursoId, UsuarioActualId));

        [HttpGet("tareas/{tareaId}")]
        public async Task<IActionResult> Tarea(int tareaId)
        {
            var error = await Verificar(await _service.CursoDeTareaActivaAsync(tareaId), "La tarea no existe.");
            if (error != null)
                return error;

            var tarea = await _service.TareaAsync(tareaId, UsuarioActualId);
            if (tarea == null)
                return NotFound(new { message = "La tarea no existe." });

            tarea.adjuntos = await _adjuntos.ListarAsync(tareaId);
            return Ok(tarea);
        }

        /// <summary>Descarga un documento que el tutor adjuntó a una tarea visible.</summary>
        [HttpGet("adjuntos/{adjuntoId}/descargar")]
        public async Task<IActionResult> DescargarAdjunto(int adjuntoId, CancellationToken ct)
        {
            var cursoId = await _adjuntos.TareaActivaAsync(adjuntoId) ? await _adjuntos.CursoDeAdjuntoAsync(adjuntoId) : null;
            var error = await Verificar(cursoId, "El documento no existe.");
            if (error != null)
                return error;

            var archivo = await _adjuntos.Abrir(adjuntoId, ct);
            if (archivo == null)
                return NotFound(new { message = "El documento no está disponible." });

            var (contenido, nombre) = archivo.Value;
            return File(contenido.Contenido, contenido.ContentType ?? "application/octet-stream", nombre);
        }

        /// <summary>
        /// Entrega o modifica la entrega: archivos nuevos (campo "archivos", se suman a los que ya hay),
        /// "enlace" para poner o cambiar el enlace y "quitarEnlace" = true para quitarlo.
        /// </summary>
        [HttpPost("tareas/{tareaId}/entrega")]
        [RequestSizeLimit(AulaService.MaxBytesPeticion)]
        [RequestFormLimits(MultipartBodyLengthLimit = AulaService.MaxBytesPeticion)]
        public async Task<IActionResult> Entregar(
            int tareaId,
            [FromForm] List<IFormFile>? archivos,
            [FromForm] string? enlace,
            [FromForm] bool quitarEnlace,
            CancellationToken ct)
        {
            var error = await Verificar(await _service.CursoDeTareaActivaAsync(tareaId), "La tarea no existe.");
            if (error != null)
                return error;

            var (tarea, mensaje) = await _service.Entregar(
                tareaId, UsuarioActualId, UsuarioActualEmail, archivos ?? [], enlace, quitarEnlace, ct);

            return tarea == null ? BadRequest(new { message = mensaje }) : Ok(tarea);
        }

        [HttpDelete("entregas/archivos/{archivoId}")]
        public async Task<IActionResult> QuitarArchivo(int archivoId)
        {
            var error = await Verificar(await _service.CursoDeArchivoPropioAsync(archivoId, UsuarioActualId), "El archivo no existe.");
            if (error != null)
                return error;

            var (exitoso, mensaje, _) = await _service.QuitarArchivo(archivoId, UsuarioActualId, UsuarioActualEmail);
            return exitoso ? Ok(new { message = mensaje }) : BadRequest(new { message = mensaje });
        }

        [HttpGet("entregas/archivos/{archivoId}/descargar")]
        public async Task<IActionResult> DescargarArchivo(int archivoId, CancellationToken ct)
        {
            var error = await Verificar(await _service.CursoDeArchivoPropioAsync(archivoId, UsuarioActualId), "El archivo no existe.");
            if (error != null)
                return error;

            var archivo = await _service.AbrirArchivo(archivoId, ct);
            if (archivo == null)
                return NotFound(new { message = "El archivo no está disponible." });

            var (contenido, nombre) = archivo.Value;
            return File(contenido.Contenido, contenido.ContentType ?? "application/octet-stream", nombre);
        }

        // ---------------------------------------------------------------
        // Permisos
        // ---------------------------------------------------------------

        /// <summary>null si el estudiante puede usar el curso; si no, la respuesta de error.</summary>
        private async Task<IActionResult?> Verificar(int? cursoId, string noExiste = "El curso no existe.")
        {
            if (cursoId == null)
                return NotFound(new { message = noExiste });

            if (!await _tutor.PuedeVerComoEstudianteAsync(cursoId.Value, UsuarioActualId))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "No tiene una matrícula activa en este curso." });

            return null;
        }
    }
}
