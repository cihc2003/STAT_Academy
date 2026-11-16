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
        private readonly TareaAdjuntoService _adjuntos;

        public TutorController(TutorService service, TareaAdjuntoService adjuntos)
        {
            _service = service;
            _adjuntos = adjuntos;
        }

        // ---------------------------------------------------------------
        // Cursos
        // ---------------------------------------------------------------

        /// <summary>Cursos asignados al tutor (Admin: todos).</summary>
        [HttpGet("cursos")]
        public async Task<IActionResult> Cursos() =>
            Ok(await _service.CursosAsignadosAsync(EsAdmin ? null : UsuarioActualId));

        [HttpGet("cursos/{cursoId}")]
        public async Task<IActionResult> Curso(int cursoId) =>
            await ConCurso(cursoId, async () => Ok(await _service.CursoAsync(cursoId)));

        [HttpGet("cursos/{cursoId}/estudiantes")]
        public async Task<IActionResult> Estudiantes(int cursoId) =>
            await ConCurso(cursoId, async () => Ok(await _service.EstudiantesAsync(cursoId)));

        [HttpGet("cursos/{cursoId}/calificaciones")]
        public async Task<IActionResult> Calificaciones(int cursoId) =>
            await ConCurso(cursoId, async () => Ok(await _service.CalificacionesAsync(cursoId)));

        /// <summary>Ajusta la nota final del estudiante; con nota = null vuelve a la calculada.</summary>
        [HttpPut("cursos/{cursoId}/estudiantes/{estudianteId}/nota-final")]
        public async Task<IActionResult> AjustarNotaFinal(int cursoId, int estudianteId, TutorNotaFinalRequest request) =>
            await ConCurso(cursoId, async () =>
            {
                var (exitoso, mensaje) = await _service.AjustarNotaFinalAsync(cursoId, estudianteId, request, UsuarioActualId, UsuarioActualEmail);
                return exitoso ? Ok(new { message = mensaje }) : BadRequest(new { message = mensaje });
            });

        // ---------------------------------------------------------------
        // Tareas
        // ---------------------------------------------------------------

        [HttpGet("cursos/{cursoId}/tareas")]
        public async Task<IActionResult> Tareas(int cursoId) =>
            await ConCurso(cursoId, async () => Ok(await _service.TareasAsync(cursoId)));

        [HttpPost("cursos/{cursoId}/tareas")]
        public async Task<IActionResult> CrearTarea(int cursoId, TutorTareaRequest request) =>
            await ConCurso(cursoId, async () =>
            {
                if (request.fechaLimite < request.fechaInicio)
                    return BadRequest(new { message = "La fecha límite no puede ser anterior a la fecha de inicio." });

                var tarea = await _service.CrearTareaAsync(cursoId, request, UsuarioActualId, UsuarioActualEmail);
                return Ok(tarea);
            });

        [HttpPut("tareas/{tareaId}")]
        public async Task<IActionResult> EditarTarea(int tareaId, TutorTareaRequest request) =>
            await ConCurso(await _service.CursoDeTareaAsync(tareaId), async () =>
            {
                if (request.fechaLimite < request.fechaInicio)
                    return BadRequest(new { message = "La fecha límite no puede ser anterior a la fecha de inicio." });

                return Ok(await _service.EditarTareaAsync(tareaId, request, UsuarioActualEmail));
            }, "La tarea no existe.");

        [HttpPatch("tareas/{tareaId}/desactivar")]
        public async Task<IActionResult> DesactivarTarea(int tareaId) =>
            await ConCurso(await _service.CursoDeTareaAsync(tareaId), async () =>
            {
                await _service.CambiarEstadoTareaAsync(tareaId, false, UsuarioActualEmail);
                return Ok(new { message = "La tarea quedó oculta para los estudiantes." });
            }, "La tarea no existe.");

        [HttpPatch("tareas/{tareaId}/activar")]
        public async Task<IActionResult> ActivarTarea(int tareaId) =>
            await ConCurso(await _service.CursoDeTareaAsync(tareaId), async () =>
            {
                await _service.CambiarEstadoTareaAsync(tareaId, true, UsuarioActualEmail);
                return Ok(new { message = "La tarea vuelve a estar visible para los estudiantes." });
            }, "La tarea no existe.");

        // ---------------------------------------------------------------
        // Entregas y calificación
        // ---------------------------------------------------------------

        [HttpGet("tareas/{tareaId}/entregas")]
        public async Task<IActionResult> Entregas(int tareaId) =>
            await ConCurso(await _service.CursoDeTareaAsync(tareaId), async () =>
            {
                var respuesta = await _service.EntregasAsync(tareaId);
                if (respuesta == null)
                    return NotFound(new { message = "La tarea no existe." });

                respuesta.adjuntos = await _adjuntos.ListarAsync(tareaId);
                respuesta.adjuntosDisponible = _adjuntos.Disponible;
                return Ok(respuesta);
            }, "La tarea no existe.");

        // ---------------------------------------------------------------
        // Documentos adjuntos a la tarea (guías, plantillas, enlaces)
        // ---------------------------------------------------------------

        /// <summary>Agrega un archivo (campo "archivo") o un enlace (campo "enlace"); "titulo" es opcional.</summary>
        [HttpPost("tareas/{tareaId}/adjuntos")]
        [RequestSizeLimit(BlogArchivoValidador.MaxBytesDocumento + 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = BlogArchivoValidador.MaxBytesDocumento + 1024 * 1024)]
        public async Task<IActionResult> AgregarAdjunto(
            int tareaId,
            [FromForm] string? titulo,
            [FromForm] IFormFile? archivo,
            [FromForm] string? enlace)
        {
            var error = await VerificarAsync(await _service.CursoDeTareaAsync(tareaId), "La tarea no existe.");
            if (error != null)
                return error;

            var (adjunto, mensaje) = await _adjuntos.Agregar(tareaId, titulo, archivo, enlace, UsuarioActualId, UsuarioActualEmail);
            return adjunto == null ? BadRequest(new { message = mensaje }) : Ok(adjunto);
        }

        [HttpDelete("adjuntos/{adjuntoId}")]
        public async Task<IActionResult> QuitarAdjunto(int adjuntoId)
        {
            var error = await VerificarAsync(await _adjuntos.CursoDeAdjuntoAsync(adjuntoId), "El documento no existe.");
            if (error != null)
                return error;

            return await _adjuntos.Quitar(adjuntoId, UsuarioActualEmail) != null
                ? Ok(new { message = "Documento quitado de la tarea." })
                : NotFound(new { message = "El documento no existe." });
        }

        [HttpGet("adjuntos/{adjuntoId}/descargar")]
        public async Task<IActionResult> DescargarAdjunto(int adjuntoId, CancellationToken ct)
        {
            var error = await VerificarAsync(await _adjuntos.CursoDeAdjuntoAsync(adjuntoId), "El documento no existe.");
            if (error != null)
                return error;

            var archivo = await _adjuntos.Abrir(adjuntoId, ct);
            if (archivo == null)
                return NotFound(new { message = "El documento no está disponible." });

            var (contenido, nombre) = archivo.Value;
            return File(contenido.Contenido, contenido.ContentType ?? "application/octet-stream", nombre);
        }

        [HttpPut("entregas/{entregaId}/calificacion")]
        public async Task<IActionResult> Calificar(int entregaId, TutorCalificarRequest request) =>
            await ConCurso(await _service.CursoDeEntregaAsync(entregaId), async () =>
            {
                var (exitoso, mensaje) = await _service.CalificarAsync(entregaId, request, UsuarioActualEmail);
                return exitoso ? Ok(new { message = mensaje }) : BadRequest(new { message = mensaje });
            }, "La entrega no existe.");

        [HttpGet("entregas/archivos/{archivoId}/descargar")]
        public async Task<IActionResult> DescargarArchivoEntrega(int archivoId, CancellationToken ct)
        {
            var error = await VerificarAsync(await _service.CursoDeArchivoEntregaAsync(archivoId), "El archivo no existe.");
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
        public async Task<IActionResult> Material(int cursoId) =>
            await ConCurso(cursoId, async () => Ok(await _service.MaterialAsync(cursoId)));

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
            var error = await VerificarAsync(cursoId);
            if (error != null)
                return error;

            var (material, mensaje) = await _service.AgregarMaterial(
                cursoId, titulo, archivo, enlace, UsuarioActualId, UsuarioActualEmail);

            return material == null ? BadRequest(new { message = mensaje }) : Ok(material);
        }

        [HttpDelete("material/{materialId}")]
        public async Task<IActionResult> QuitarMaterial(int materialId)
        {
            var error = await VerificarAsync(await _service.CursoDeMaterialAsync(materialId), "El material no existe.");
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
        private async Task<IActionResult?> VerificarAsync(int? cursoId, string noExiste = "El curso no existe.")
        {
            if (cursoId == null)
                return NotFound(new { message = noExiste });

            var (existe, tutorId) = await _service.TutorDelCursoAsync(cursoId.Value);

            if (!existe)
                return NotFound(new { message = "El curso no existe." });

            if (!EsAdmin && tutorId != UsuarioActualId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Este curso no está asignado a usted." });

            return null;
        }

        private async Task<IActionResult> ConCurso(int? cursoId, Func<Task<IActionResult>> accion, string noExiste = "El curso no existe.") =>
            await VerificarAsync(cursoId, noExiste) ?? await accion();
    }
}
