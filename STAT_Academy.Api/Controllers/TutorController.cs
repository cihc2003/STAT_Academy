using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Tutor;

namespace STAT_Academy.Api.Controllers
{
    /// <summary>
    /// Panel del tutor. Un tutor solo gestiona los cursos donde CURSO.FK_Tutor es él;
    /// un Admin puede gestionar cualquiera.
    /// </summary>
    [Authorize(Roles = Roles.AdminOTutor)]
    [Route("api/[controller]")]
    public class TutorController : ApiControllerBase
    {
        private readonly TutorService _service;

        public TutorController(TutorService service)
        {
            _service = service;
        }

        // ---------------------------------------------------------------
        // Cursos
        // ---------------------------------------------------------------

        /// <summary>Cursos asignados al tutor (Admin: todos).</summary>
        [HttpGet("cursos")]
        public IActionResult Cursos() =>
            Ok(_service.CursosAsignados(EsAdmin ? null : UsuarioActualId));

        [HttpGet("cursos/{cursoId}")]
        public IActionResult Curso(int cursoId) =>
            ConCurso(cursoId, () => Ok(_service.Curso(cursoId)));

        [HttpGet("cursos/{cursoId}/estudiantes")]
        public IActionResult Estudiantes(int cursoId) =>
            ConCurso(cursoId, () => Ok(_service.Estudiantes(cursoId)));

        [HttpGet("cursos/{cursoId}/calificaciones")]
        public IActionResult Calificaciones(int cursoId) =>
            ConCurso(cursoId, () => Ok(_service.Calificaciones(cursoId)));

        // ---------------------------------------------------------------
        // Tareas
        // ---------------------------------------------------------------

        [HttpGet("cursos/{cursoId}/tareas")]
        public IActionResult Tareas(int cursoId) =>
            ConCurso(cursoId, () => Ok(_service.Tareas(cursoId)));

        [HttpPost("cursos/{cursoId}/tareas")]
        public IActionResult CrearTarea(int cursoId, TutorTareaRequest request) =>
            ConCurso(cursoId, () =>
            {
                if (request.fechaLimite < request.fechaInicio)
                    return BadRequest(new { message = "La fecha límite no puede ser anterior a la fecha de inicio." });

                var tarea = _service.CrearTarea(cursoId, request, UsuarioActualId, UsuarioActualEmail);
                return Ok(tarea);
            });

        [HttpPut("tareas/{tareaId}")]
        public IActionResult EditarTarea(int tareaId, TutorTareaRequest request) =>
            ConCurso(_service.CursoDeTarea(tareaId), () =>
            {
                if (request.fechaLimite < request.fechaInicio)
                    return BadRequest(new { message = "La fecha límite no puede ser anterior a la fecha de inicio." });

                return Ok(_service.EditarTarea(tareaId, request, UsuarioActualEmail));
            }, "La tarea no existe.");

        [HttpPatch("tareas/{tareaId}/desactivar")]
        public IActionResult DesactivarTarea(int tareaId) =>
            ConCurso(_service.CursoDeTarea(tareaId), () =>
            {
                _service.CambiarEstadoTarea(tareaId, false, UsuarioActualEmail);
                return Ok(new { message = "La tarea quedó oculta para los estudiantes." });
            }, "La tarea no existe.");

        [HttpPatch("tareas/{tareaId}/activar")]
        public IActionResult ActivarTarea(int tareaId) =>
            ConCurso(_service.CursoDeTarea(tareaId), () =>
            {
                _service.CambiarEstadoTarea(tareaId, true, UsuarioActualEmail);
                return Ok(new { message = "La tarea vuelve a estar visible para los estudiantes." });
            }, "La tarea no existe.");

        // ---------------------------------------------------------------
        // Entregas y calificación
        // ---------------------------------------------------------------

        [HttpGet("tareas/{tareaId}/entregas")]
        public IActionResult Entregas(int tareaId) =>
            ConCurso(_service.CursoDeTarea(tareaId), () => Ok(_service.Entregas(tareaId)), "La tarea no existe.");

        [HttpPut("entregas/{entregaId}/calificacion")]
        public IActionResult Calificar(int entregaId, TutorCalificarRequest request) =>
            ConCurso(_service.CursoDeEntrega(entregaId), () =>
            {
                var (exitoso, mensaje) = _service.Calificar(entregaId, request, UsuarioActualEmail);
                return exitoso ? Ok(new { message = mensaje }) : BadRequest(new { message = mensaje });
            }, "La entrega no existe.");

        [HttpGet("entregas/archivos/{archivoId}/descargar")]
        public async Task<IActionResult> DescargarArchivoEntrega(int archivoId, CancellationToken ct)
        {
            var error = Verificar(_service.CursoDeArchivoEntrega(archivoId), "El archivo no existe.");
            if (error != null)
                return error;

            var archivo = await _service.AbrirArchivoEntrega(archivoId, ct);
            if (archivo == null)
                return NotFound(new { message = "El archivo no está disponible." });

            var (contenido, nombre) = archivo.Value;
            return File(contenido.Contenido, contenido.ContentType ?? "application/octet-stream", nombre);
        }

        // ---------------------------------------------------------------
        // Material
        // ---------------------------------------------------------------

        [HttpGet("cursos/{cursoId}/material")]
        public IActionResult Material(int cursoId) =>
            ConCurso(cursoId, () => Ok(_service.Material(cursoId)));

        /// <summary>Agrega material: un archivo (campo "archivo") o un enlace (campo "enlace").</summary>
        [HttpPost("cursos/{cursoId}/material")]
        [RequestSizeLimit(BlogArchivoValidador.MaxBytesDocumento + 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = BlogArchivoValidador.MaxBytesDocumento + 1024 * 1024)]
        public async Task<IActionResult> AgregarMaterial(
            int cursoId,
            [FromForm] string titulo,
            [FromForm] IFormFile? archivo,
            [FromForm] string? enlace)
        {
            var error = Verificar(cursoId);
            if (error != null)
                return error;

            var (material, mensaje) = await _service.AgregarMaterial(
                cursoId, titulo, archivo, enlace, UsuarioActualId, UsuarioActualEmail);

            return material == null ? BadRequest(new { message = mensaje }) : Ok(material);
        }

        [HttpDelete("material/{materialId}")]
        public async Task<IActionResult> QuitarMaterial(int materialId)
        {
            var error = Verificar(_service.CursoDeMaterial(materialId), "El material no existe.");
            if (error != null)
                return error;

            return await _service.QuitarMaterial(materialId, UsuarioActualEmail)
                ? Ok(new { message = "Material quitado del curso." })
                : NotFound(new { message = "El material no existe." });
        }

        // ---------------------------------------------------------------
        // Permisos
        // ---------------------------------------------------------------

        /// <summary>null si el usuario puede gestionar el curso; si no, la respuesta de error.</summary>
        private IActionResult? Verificar(int? cursoId, string noExiste = "El curso no existe.")
        {
            if (cursoId == null)
                return NotFound(new { message = noExiste });

            var (existe, tutorId) = _service.TutorDelCurso(cursoId.Value);

            if (!existe)
                return NotFound(new { message = "El curso no existe." });

            if (!EsAdmin && tutorId != UsuarioActualId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Este curso no está asignado a usted." });

            return null;
        }

        private IActionResult ConCurso(int? cursoId, Func<IActionResult> accion, string noExiste = "El curso no existe.") =>
            Verificar(cursoId, noExiste) ?? accion();
    }
}
