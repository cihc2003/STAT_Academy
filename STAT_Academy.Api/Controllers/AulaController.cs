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

        public AulaController(AulaService service, TutorService tutor)
        {
            _service = service;
            _tutor = tutor;
        }

        /// <summary>Tareas del curso con el estado de la entrega propia, progreso y nota final.</summary>
        [HttpGet("cursos/{cursoId}")]
        public IActionResult Curso(int cursoId) =>
            ConCurso(cursoId, () => Ok(_service.Curso(cursoId, UsuarioActualId)));

        [HttpGet("tareas/{tareaId}")]
        public IActionResult Tarea(int tareaId) =>
            ConCurso(_service.CursoDeTareaActiva(tareaId), () => Ok(_service.Tarea(tareaId, UsuarioActualId)), "La tarea no existe.");

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
            var error = Verificar(_service.CursoDeTareaActiva(tareaId), "La tarea no existe.");
            if (error != null)
                return error;

            var (tarea, mensaje) = await _service.Entregar(
                tareaId, UsuarioActualId, UsuarioActualEmail, archivos ?? [], enlace, quitarEnlace, ct);

            return tarea == null ? BadRequest(new { message = mensaje }) : Ok(tarea);
        }

        [HttpDelete("entregas/archivos/{archivoId}")]
        public async Task<IActionResult> QuitarArchivo(int archivoId)
        {
            var error = Verificar(_service.CursoDeArchivoPropio(archivoId, UsuarioActualId), "El archivo no existe.");
            if (error != null)
                return error;

            var (exitoso, mensaje, _) = await _service.QuitarArchivo(archivoId, UsuarioActualId, UsuarioActualEmail);
            return exitoso ? Ok(new { message = mensaje }) : BadRequest(new { message = mensaje });
        }

        [HttpGet("entregas/archivos/{archivoId}/descargar")]
        public async Task<IActionResult> DescargarArchivo(int archivoId, CancellationToken ct)
        {
            var error = Verificar(_service.CursoDeArchivoPropio(archivoId, UsuarioActualId), "El archivo no existe.");
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
        private IActionResult? Verificar(int? cursoId, string noExiste = "El curso no existe.")
        {
            if (cursoId == null)
                return NotFound(new { message = noExiste });

            if (!_tutor.PuedeVerComoEstudiante(cursoId.Value, UsuarioActualId))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "No tiene una matrícula activa en este curso." });

            return null;
        }

        private IActionResult ConCurso(int? cursoId, Func<IActionResult> accion, string noExiste = "El curso no existe.") =>
            Verificar(cursoId, noExiste) ?? accion();
    }
}
