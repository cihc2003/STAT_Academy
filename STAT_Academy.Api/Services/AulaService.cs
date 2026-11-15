using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Storage;
using STAT_Academy.DTOs.Aula;

namespace STAT_Academy.Api.Services
{
    /// <summary>
    /// Aula del estudiante: sus tareas con el estado de su entrega, entregar (archivos y/o enlace)
    /// y sus calificaciones con la nota final. Los permisos (matrícula activa y curso activo)
    /// los valida <see cref="Controllers.AulaController"/> antes de llamar aquí.
    /// Reglas de entrega: se puede entregar después de la fecha límite (queda "fuera de plazo")
    /// y cambiar la entrega mientras no esté calificada.
    /// </summary>
    public class AulaService
    {
        public const int MaxArchivosPorEntrega = 5;
        public const int MaxLargoEnlace = 255;

        /// <summary>Tamaño máximo de una petición de entrega (5 archivos de hasta 10 MB + margen).</summary>
        public const long MaxBytesPeticion = MaxArchivosPorEntrega * BlogArchivoValidador.MaxBytesDocumento + 1024 * 1024;

        private const string SinEntregar = "Sin entregar";

        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly IBlobStorageService _blob;
        private readonly NotaFinalService _notaFinal;
        private readonly CorreoService _correo;
        private readonly ILogger<AulaService> _logger;

        public AulaService(
            ApplicationDbContext context,
            AuditoriaService auditoria,
            IBlobStorageService blob,
            NotaFinalService notaFinal,
            CorreoService correo,
            ILogger<AulaService> logger)
        {
            _context = context;
            _auditoria = auditoria;
            _blob = blob;
            _notaFinal = notaFinal;
            _correo = correo;
            _logger = logger;
        }

        // ==================================================================
        // Lecturas
        // ==================================================================

        public AulaCursoResponse? Curso(int cursoId, int estudianteId)
        {
            var curso = _context.Curso.AsNoTracking().FirstOrDefault(c => c.id == cursoId);
            if (curso == null)
                return null;

            var tareas = TareasActivas(cursoId);
            var entregas = Entregas(tareas.Select(t => t.ID).ToList(), estudianteId);

            var matriculaId = _context.EstudianteCurso
                .Where(m => m.CursoId == cursoId && m.EstudianteId == estudianteId)
                .OrderByDescending(m => m.IdMatricula)
                .Select(m => (int?)m.IdMatricula)
                .FirstOrDefault();

            var ajuste = matriculaId == null ? null : _notaFinal.Ajuste(matriculaId.Value);

            return new AulaCursoResponse
            {
                cursoId = curso.id,
                curso = curso.nombre,
                fechaInicio = curso.fechaInicio,
                fechaFin = curso.fechaFin,
                progreso = CursoModel.ProgresoDe(curso.fechaInicio, curso.fechaFin),
                tareas = tareas.Select(t => Map(t, curso, entregas.GetValueOrDefault(t.ID))).ToList(),
                notaFinal = NotaFinalCalculo.Calcular(tareas, entregas.Values, ajuste)
            };
        }

        public AulaTareaResponse? Tarea(int tareaId, int estudianteId)
        {
            var tarea = _context.Tarea.AsNoTracking().FirstOrDefault(t => t.ID == tareaId && t.Estado != false);
            if (tarea?.CursoId == null)
                return null;

            var curso = _context.Curso.AsNoTracking().First(c => c.id == tarea.CursoId.Value);
            var entrega = Entregas([tareaId], estudianteId).GetValueOrDefault(tareaId);

            return Map(tarea, curso, entrega);
        }

        /// <summary>Curso de una tarea visible para estudiantes (activa), o null.</summary>
        public int? CursoDeTareaActiva(int tareaId) =>
            _context.Tarea.Where(t => t.ID == tareaId && t.Estado != false).Select(t => t.CursoId).FirstOrDefault();

        /// <summary>Curso del archivo, solo si la entrega es del estudiante indicado.</summary>
        public int? CursoDeArchivoPropio(int archivoId, int estudianteId) =>
            (from a in _context.EntregaTareaArchivo
             join e in _context.EntregaTarea on a.fk_Entrega_Tarea equals e.id
             join t in _context.Tarea on e.fk_Tarea equals t.ID
             where a.id == archivoId && a.estado && e.fk_Estudiante == estudianteId
             select t.CursoId).FirstOrDefault();

        public async Task<(ArchivoDescarga contenido, string nombre)?> AbrirArchivo(int archivoId, CancellationToken ct)
        {
            var archivo = _context.EntregaTareaArchivo.AsNoTracking().FirstOrDefault(a => a.id == archivoId && a.estado);
            if (archivo == null)
                return null;

            try
            {
                return (await _blob.DescargarAsync(ContenedorBlob.Documentos, archivo.archivo_URL, ct), archivo.nombre_Archivo ?? "entrega");
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("El archivo de entrega {ArchivoId} no existe en Blob Storage.", archivoId);
                return null;
            }
        }

        // ==================================================================
        // Entregar
        // ==================================================================

        /// <summary>
        /// Crea la entrega o la modifica: agrega archivos y pone, cambia o quita el enlace.
        /// Una entrega calificada ya no se puede modificar.
        /// </summary>
        public async Task<(AulaTareaResponse? tarea, string? error)> Entregar(
            int tareaId, int estudianteId, string estudianteEmail,
            IReadOnlyList<IFormFile> archivos, string? enlace, bool quitarEnlace, CancellationToken ct)
        {
            var tarea = _context.Tarea.AsNoTracking().FirstOrDefault(t => t.ID == tareaId && t.Estado != false);
            if (tarea?.CursoId == null)
                return (null, "La tarea no existe.");

            var entrega = _context.EntregaTarea
                .Include(e => e.Archivos.Where(a => a.estado))
                .Where(e => e.fk_Tarea == tareaId && e.fk_Estudiante == estudianteId)
                .OrderByDescending(e => e.id)
                .FirstOrDefault();

            if (entrega?.calificacion != null)
                return (null, "La tarea ya fue calificada; la entrega no se puede modificar.");

            // --- Enlace ---------------------------------------------------
            string? enlaceNuevo = entrega?.archivo_Entrega_URL;

            if (quitarEnlace)
                enlaceNuevo = null;

            if (!string.IsNullOrWhiteSpace(enlace))
            {
                if (!Uri.TryCreate(enlace.Trim(), UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                    return (null, "El enlace debe ser una dirección web completa (https://…).");

                if (uri.AbsoluteUri.Length > MaxLargoEnlace)
                    return (null, $"El enlace es demasiado largo (máx. {MaxLargoEnlace} caracteres).");

                enlaceNuevo = uri.AbsoluteUri;
            }

            // --- Archivos -------------------------------------------------
            var nuevos = archivos.Where(a => a is { Length: > 0 }).ToList();
            var actuales = entrega?.Archivos.Count ?? 0;

            if (actuales + nuevos.Count > MaxArchivosPorEntrega)
                return (null, $"Una entrega admite como máximo {MaxArchivosPorEntrega} archivos (ya tiene {actuales}).");

            // Quitar el único enlace de una entrega sin archivos: la tarea vuelve a "sin entregar".
            if (entrega?.fecha_Entrega != null && nuevos.Count == 0 && actuales == 0 && enlaceNuevo == null)
            {
                entrega.archivo_Entrega_URL = null;
                entrega.fecha_Entrega = null;
                entrega.estado = null;
                await _context.SaveChangesAsync(ct);

                _auditoria.Registrar("ENTREGA_TAREA", "DISABLE",
                    $"Entrega retirada de la tarea {Recortar(tarea.Titulo ?? "")}", estudianteEmail, entrega.id);

                return (Tarea(tareaId, estudianteId), null);
            }

            if (nuevos.Count == 0 && !TutorService.EsEnlace(enlaceNuevo) && actuales == 0)
                return (null, "Adjunte al menos un archivo o escriba un enlace.");

            if (nuevos.Count == 0 && enlaceNuevo == entrega?.archivo_Entrega_URL)
                return (null, "No hay cambios en la entrega.");

            // Se validan todos antes de subir ninguno.
            var validados = new List<BlogArchivoValidador.ArchivoValidado>();
            foreach (var archivo in nuevos)
            {
                var (valido, error) = await BlogArchivoValidador.Validar(archivo);
                if (error != null)
                    return (null, error);
                validados.Add(valido!);
            }

            var subidos = new List<(BlogArchivoValidador.ArchivoValidado archivo, string blob)>();
            try
            {
                foreach (var v in validados)
                {
                    // Contenedor PRIVADO: las entregas solo las ven el estudiante, el tutor y Admin.
                    var blob = await _blob.SubirPrivadoAsync(v.Contenido, v.Extension, v.Mime, $"entregas/{tareaId}/{estudianteId}", ct);
                    subidos.Add((v, blob));
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "No se pudo subir la entrega de la tarea {TareaId}.", tareaId);
                await BorrarBlobs(subidos.Select(s => s.blob));
                return (null, "No se pudieron guardar los archivos. Inténtelo de nuevo en unos minutos.");
            }

            var primeraVez = entrega == null || entrega.fecha_Entrega == null;

            if (entrega == null)
            {
                entrega = new EntregaTareaModel { fk_Tarea = tareaId, fk_Estudiante = estudianteId };
                _context.EntregaTarea.Add(entrega);
            }

            entrega.archivo_Entrega_URL = enlaceNuevo;
            entrega.fecha_Entrega = CursoModel.AhoraCostaRica;
            entrega.estado = EntregaTareaModel.EstadoEntregada;

            foreach (var (archivo, blob) in subidos)
            {
                entrega.Archivos.Add(new EntregaTareaArchivoModel
                {
                    nombre_Archivo = archivo.NombreOriginal,
                    archivo_URL = blob,
                    tipo_Archivo = TutorService.TipoMaterial(archivo.Extension),
                    estado = true
                });
            }

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "No se registró la entrega de la tarea {TareaId}.", tareaId);
                await BorrarBlobs(subidos.Select(s => s.blob));
                return (null, "No se pudo registrar la entrega. Inténtelo de nuevo.");
            }

            _auditoria.Registrar("ENTREGA_TAREA", primeraVez ? "CREATE" : "UPDATE",
                $"Entrega de la tarea {Recortar(tarea.Titulo ?? "")} ({subidos.Count} archivo(s){(TutorService.EsEnlace(enlaceNuevo) ? " y enlace" : "")})",
                estudianteEmail, entrega.id);

            AvisarTutor(tarea, estudianteId, primeraVez);

            return (Tarea(tareaId, estudianteId), null);
        }

        /// <summary>Quita un archivo de la entrega propia (si no está calificada).</summary>
        public async Task<(bool exitoso, string mensaje, int? tareaId)> QuitarArchivo(int archivoId, int estudianteId, string estudianteEmail)
        {
            var archivo = _context.EntregaTareaArchivo.FirstOrDefault(a => a.id == archivoId && a.estado);
            var entrega = archivo == null
                ? null
                : _context.EntregaTarea
                    .Include(e => e.Archivos.Where(a => a.estado))
                    .FirstOrDefault(e => e.id == archivo.fk_Entrega_Tarea && e.fk_Estudiante == estudianteId);

            if (archivo == null || entrega == null)
                return (false, "El archivo no existe.", null);

            if (entrega.calificacion != null)
                return (false, "La tarea ya fue calificada; la entrega no se puede modificar.", entrega.fk_Tarea);

            archivo.estado = false;

            // Sin archivos ni enlace, la tarea vuelve a quedar "sin entregar".
            var quedan = entrega.Archivos.Count(a => a.id != archivo.id);
            var retirada = quedan == 0 && !TutorService.EsEnlace(entrega.archivo_Entrega_URL);
            if (retirada)
            {
                entrega.fecha_Entrega = null;
                entrega.estado = null;
            }

            _context.SaveChanges();
            await BorrarBlobs([archivo.archivo_URL]);

            _auditoria.Registrar("ENTREGA_TAREA_ARCHIVO", "DISABLE",
                $"Archivo quitado de la entrega: {Recortar(archivo.nombre_Archivo ?? "")}", estudianteEmail, archivo.id);

            return (true, retirada ? "Archivo quitado. La tarea quedó sin entregar." : "Archivo quitado de la entrega.", entrega.fk_Tarea);
        }

        // ==================================================================
        // Ayudantes
        // ==================================================================

        private List<TareaModel> TareasActivas(int cursoId) =>
            _context.Tarea
                .AsNoTracking()
                .Where(t => t.CursoId == cursoId && t.Estado != false)
                .OrderBy(t => t.Fecha_Limite)
                .ToList();

        /// <summary>La entrega más reciente del estudiante por tarea, con sus archivos activos.</summary>
        private Dictionary<int, EntregaTareaModel> Entregas(List<int> tareaIds, int estudianteId) =>
            _context.EntregaTarea
                .AsNoTracking()
                .Include(e => e.Archivos.Where(a => a.estado))
                .Where(e => e.fk_Estudiante == estudianteId && e.fk_Tarea != null && tareaIds.Contains(e.fk_Tarea.Value))
                .ToList()
                .GroupBy(e => e.fk_Tarea!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.id).First());

        private static AulaTareaResponse Map(TareaModel t, CursoModel curso, EntregaTareaModel? e)
        {
            var entregada = e?.fecha_Entrega != null;
            var vencida = NotaFinalCalculo.Vencida(t);

            return new AulaTareaResponse
            {
                id = t.ID,
                cursoId = curso.id,
                curso = curso.nombre,
                titulo = t.Titulo ?? "Tarea sin título",
                descripcion = t.Descripcion ?? "",
                fechaInicio = t.Fecha_Inicio,
                fechaLimite = t.Fecha_Limite,
                // Una tarea publicada después del fin del curso no cae en ninguna semana del curso.
                semana = t.Fecha_Inicio?.Date > curso.fechaFin.Date ? null : CursoModel.SemanaDe(curso.fechaInicio, t.Fecha_Inicio),
                vencida = vencida,
                estado = !entregada ? SinEntregar
                    : e!.calificacion != null ? EntregaTareaModel.EstadoCalificada
                    : EntregaTareaModel.EstadoEntregada,
                fechaEntrega = entregada ? e!.fecha_Entrega : null,
                tarde = entregada && t.Fecha_Limite != null && e!.fecha_Entrega > t.Fecha_Limite,
                calificacion = e?.calificacion,
                // La retroalimentación solo se muestra junto con la nota.
                feedback = e?.calificacion != null ? e.feedback : null,
                enlace = entregada && TutorService.EsEnlace(e!.archivo_Entrega_URL) ? e.archivo_Entrega_URL : null,
                archivos = entregada
                    ? e!.Archivos.Select(a => new AulaArchivoResponse
                    {
                        id = a.id,
                        nombre = a.nombre_Archivo ?? "archivo",
                        tipo = a.tipo_Archivo ?? ""
                    }).ToList()
                    : new(),
                puedeModificar = e?.calificacion == null,
                cuentaComoCero = !entregada && vencida
            };
        }

        /// <summary>Correo al tutor del curso, en segundo plano. Es complementario: si falla, la entrega ya quedó guardada.</summary>
        private void AvisarTutor(TareaModel tarea, int estudianteId, bool primeraVez)
        {
            try
            {
                var datos = (from c in _context.Curso
                             join u in _context.Usuario on c.fk_tutor equals u.id
                             where c.id == tarea.CursoId
                             select new { curso = c.nombre, u.email, u.nombre })
                            .AsNoTracking()
                            .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(datos?.email))
                    return;

                var estudiante = _context.Usuario.Where(u => u.id == estudianteId)
                    .Select(u => u.nombre ?? u.email).FirstOrDefault() ?? "Un estudiante";

                CorreoService.EnSegundoPlano(_logger, $"la entrega de la tarea {tarea.ID}", () =>
                    _correo.EnviarEntregaRecibida(datos.email, datos.nombre ?? "", estudiante,
                        datos.curso, tarea.Titulo ?? "Tarea", tarea.ID, primeraVez));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo preparar el aviso de la entrega de la tarea {TareaId}.", tarea.ID);
            }
        }

        private async Task BorrarBlobs(IEnumerable<string> blobs)
        {
            foreach (var blob in blobs)
            {
                try
                {
                    await _blob.EliminarAsync(ContenedorBlob.Documentos, blob);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo borrar el blob {Blob} de una entrega.", blob);
                }
            }
        }

        private static string Recortar(string texto) =>
            texto.Length > 120 ? texto[..120] + "…" : texto;
    }
}
