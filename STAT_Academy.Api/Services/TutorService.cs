using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Storage;
using STAT_Academy.DTOs.Tutor;

namespace STAT_Academy.Api.Services
{
    /// <summary>
    /// Gestión de cursos por parte del tutor asignado (CURSO.FK_Tutor):
    /// tareas, entregas y calificaciones, estudiantes y material.
    /// Los permisos los valida <see cref="Controllers.TutorController"/> antes de llamar aquí.
    /// </summary>
    public class TutorService
    {
        private const string MatriculaActiva = "Activo";

        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly IBlobStorageService _blob;
        private readonly ILogger<TutorService> _logger;

        public TutorService(
            ApplicationDbContext context,
            AuditoriaService auditoria,
            IBlobStorageService blob,
            ILogger<TutorService> logger)
        {
            _context = context;
            _auditoria = auditoria;
            _blob = blob;
            _logger = logger;
        }

        // ==================================================================
        // Permisos
        // ==================================================================

        /// <summary>Tutor asignado al curso, o null si el curso no existe.</summary>
        public (bool existe, int tutorId) TutorDelCurso(int cursoId)
        {
            var tutor = _context.Curso
                .Where(c => c.id == cursoId)
                .Select(c => (int?)c.fk_tutor)
                .FirstOrDefault();

            return (tutor != null, tutor ?? 0);
        }

        public int? CursoDeTarea(int tareaId) =>
            _context.Tarea.Where(t => t.ID == tareaId).Select(t => t.CursoId).FirstOrDefault();

        public int? CursoDeEntrega(int entregaId) =>
            (from e in _context.EntregaTarea
             join t in _context.Tarea on e.fk_Tarea equals t.ID
             where e.id == entregaId
             select t.CursoId).FirstOrDefault();

        public int? CursoDeArchivoEntrega(int archivoId) =>
            (from a in _context.EntregaTareaArchivo
             join e in _context.EntregaTarea on a.fk_Entrega_Tarea equals e.id
             join t in _context.Tarea on e.fk_Tarea equals t.ID
             where a.id == archivoId && a.estado
             select t.CursoId).FirstOrDefault();

        public int? CursoDeMaterial(int materialId) =>
            _context.MaterialCurso.Where(m => m.id == materialId).Select(m => m.fk_Curso).FirstOrDefault();

        // ==================================================================
        // Cursos asignados
        // ==================================================================

        /// <param name="tutorId">null = todos los cursos (Admin).</param>
        public List<TutorCursoResponse> CursosAsignados(int? tutorId)
        {
            var cursos = _context.Curso
                .AsNoTracking()
                .Where(c => tutorId == null || c.fk_tutor == tutorId)
                .OrderByDescending(c => c.estado)
                .ThenBy(c => c.nombre)
                .ToList();

            return cursos.Select(ResumenCurso).ToList();
        }

        public TutorCursoResponse? Curso(int cursoId)
        {
            var curso = _context.Curso.AsNoTracking().FirstOrDefault(c => c.id == cursoId);
            return curso == null ? null : ResumenCurso(curso);
        }

        private TutorCursoResponse ResumenCurso(CursoModel c)
        {
            var tareasActivas = _context.Tarea.Where(t => t.CursoId == c.id && t.Estado != false);

            return new TutorCursoResponse
            {
                id = c.id,
                nombre = c.nombre,
                descripcion = c.descripcion,
                estado = c.estado,
                fechaInicio = c.fechaInicio,
                fechaFin = c.fechaFin,
                duracionSemanas = c.duracionSemanas,
                estudiantes = _context.EstudianteCurso.Count(m => m.CursoId == c.id && m.Estado == MatriculaActiva),
                tareas = tareasActivas.Count(),
                entregasPorCalificar = _context.EntregaTarea.Count(e =>
                    tareasActivas.Any(t => t.ID == e.fk_Tarea) && e.calificacion == null && e.fecha_Entrega != null),
                material = _context.MaterialCurso.Count(m => m.fk_Curso == c.id && m.estado == true)
            };
        }

        // ==================================================================
        // Estudiantes
        // ==================================================================

        public List<TutorEstudianteResponse> Estudiantes(int cursoId)
        {
            var matriculas = (from m in _context.EstudianteCurso
                              join u in _context.Usuario on m.EstudianteId equals u.id
                              where m.CursoId == cursoId
                              orderby u.nombre
                              select new { m, u.nombre, u.email })
                             .AsNoTracking()
                             .ToList();

            var tareaIds = TareasActivasIds(cursoId);
            var entregas = EntregasDelCurso(tareaIds);

            return matriculas.Select(x =>
            {
                var propias = entregas.Where(e => e.fk_Estudiante == x.m.EstudianteId).ToList();
                var notas = propias.Where(e => e.calificacion != null).Select(e => e.calificacion!.Value).ToList();

                return new TutorEstudianteResponse
                {
                    matriculaId = x.m.IdMatricula,
                    estudianteId = x.m.EstudianteId,
                    nombre = x.nombre ?? x.email ?? $"Estudiante #{x.m.EstudianteId}",
                    email = x.email ?? "",
                    fechaMatricula = x.m.Fecha_Matricula,
                    estadoMatricula = x.m.Estado ?? "",
                    progreso = x.m.Progreso,
                    tareasEntregadas = propias.Count(e => e.fecha_Entrega != null),
                    tareasTotales = tareaIds.Count,
                    promedio = notas.Count > 0 ? Math.Round(notas.Average(), 2) : null
                };
            }).ToList();
        }

        // ==================================================================
        // Calificaciones (tabla estudiantes × tareas)
        // ==================================================================

        public TutorCalificacionesResponse Calificaciones(int cursoId)
        {
            var tareas = _context.Tarea
                .AsNoTracking()
                .Where(t => t.CursoId == cursoId && t.Estado != false)
                .OrderBy(t => t.Fecha_Limite)
                .ToList();

            var estudiantes = (from m in _context.EstudianteCurso
                               join u in _context.Usuario on m.EstudianteId equals u.id
                               where m.CursoId == cursoId && m.Estado == MatriculaActiva
                               orderby u.nombre
                               select new { u.id, u.nombre, u.email })
                              .AsNoTracking()
                              .ToList();

            var entregas = EntregasDelCurso(tareas.Select(t => t.ID).ToList());

            return new TutorCalificacionesResponse
            {
                tareas = tareas.Select(t => new TutorTareaColumna
                {
                    id = t.ID,
                    titulo = t.Titulo ?? $"Tarea #{t.ID}",
                    fechaLimite = t.Fecha_Limite
                }).ToList(),

                estudiantes = estudiantes.Select(est =>
                {
                    var celdas = tareas.Select(t =>
                    {
                        var e = entregas.FirstOrDefault(x => x.fk_Tarea == t.ID && x.fk_Estudiante == est.id);
                        return new TutorCeldaCalificacion
                        {
                            tareaId = t.ID,
                            entregaId = e?.id,
                            estado = EstadoEntrega(e),
                            calificacion = e?.calificacion
                        };
                    }).ToList();

                    var notas = celdas.Where(c => c.calificacion != null).Select(c => c.calificacion!.Value).ToList();

                    return new TutorFilaCalificaciones
                    {
                        estudianteId = est.id,
                        nombre = est.nombre ?? est.email ?? $"Estudiante #{est.id}",
                        email = est.email ?? "",
                        notas = celdas,
                        promedio = notas.Count > 0 ? Math.Round(notas.Average(), 2) : null
                    };
                }).ToList()
            };
        }

        // ==================================================================
        // Tareas
        // ==================================================================

        public List<TutorTareaResponse> Tareas(int cursoId)
        {
            var inicioCurso = _context.Curso.Where(c => c.id == cursoId).Select(c => c.fechaInicio).First();
            var estudiantes = _context.EstudianteCurso.Count(m => m.CursoId == cursoId && m.Estado == MatriculaActiva);

            var tareas = _context.Tarea
                .AsNoTracking()
                .Where(t => t.CursoId == cursoId)
                .OrderBy(t => t.Fecha_Inicio)
                .ToList();

            var entregas = EntregasDelCurso(tareas.Select(t => t.ID).ToList());

            return tareas.Select(t => MapTarea(t, inicioCurso, estudiantes, entregas)).ToList();
        }

        public TutorTareaResponse CrearTarea(int cursoId, TutorTareaRequest request, int autorId, string autorEmail)
        {
            var tarea = new TareaModel
            {
                CursoId = cursoId,
                Titulo = request.titulo.Trim(),
                Descripcion = request.descripcion?.Trim() ?? "",
                Fecha_Inicio = request.fechaInicio,
                Fecha_Limite = request.fechaLimite,
                Entregada = false,
                Estado = true,
                AutorId = autorId
            };

            _context.Tarea.Add(tarea);
            _context.SaveChanges();

            _auditoria.Registrar("TAREA", "CREATE", $"Tarea creada en curso {cursoId}: {Recortar(tarea.Titulo)}", autorEmail, tarea.ID);

            return Tareas(cursoId).First(t => t.id == tarea.ID);
        }

        public TutorTareaResponse? EditarTarea(int tareaId, TutorTareaRequest request, string usuarioEmail)
        {
            var tarea = _context.Tarea.FirstOrDefault(t => t.ID == tareaId);
            if (tarea == null || tarea.CursoId == null)
                return null;

            tarea.Titulo = request.titulo.Trim();
            tarea.Descripcion = request.descripcion?.Trim() ?? "";
            tarea.Fecha_Inicio = request.fechaInicio;
            tarea.Fecha_Limite = request.fechaLimite;

            _context.SaveChanges();

            _auditoria.Registrar("TAREA", "UPDATE", $"Tarea actualizada: {Recortar(tarea.Titulo)}", usuarioEmail, tarea.ID);

            return Tareas(tarea.CursoId.Value).First(t => t.id == tareaId);
        }

        public bool CambiarEstadoTarea(int tareaId, bool activa, string usuarioEmail)
        {
            var tarea = _context.Tarea.FirstOrDefault(t => t.ID == tareaId);
            if (tarea == null)
                return false;

            tarea.Estado = activa;
            _context.SaveChanges();

            _auditoria.Registrar("TAREA", activa ? "ENABLE" : "DISABLE",
                $"Tarea {(activa ? "publicada" : "ocultada")}: {Recortar(tarea.Titulo ?? "")}", usuarioEmail, tarea.ID);

            return true;
        }

        // ==================================================================
        // Entregas y calificación
        // ==================================================================

        public TutorEntregasResponse? Entregas(int tareaId)
        {
            var tarea = _context.Tarea.AsNoTracking().FirstOrDefault(t => t.ID == tareaId);
            if (tarea?.CursoId == null)
                return null;

            var cursoId = tarea.CursoId.Value;
            var curso = _context.Curso.AsNoTracking().First(c => c.id == cursoId);

            var estudiantes = (from m in _context.EstudianteCurso
                               join u in _context.Usuario on m.EstudianteId equals u.id
                               where m.CursoId == cursoId && m.Estado == MatriculaActiva
                               orderby u.nombre
                               select new { u.id, u.nombre, u.email })
                              .AsNoTracking()
                              .ToList();

            var entregas = _context.EntregaTarea
                .AsNoTracking()
                .Include(e => e.Archivos.Where(a => a.estado))
                .Where(e => e.fk_Tarea == tareaId)
                .ToList();

            var estudiantesActivos = estudiantes.Count;

            // Estudiantes matriculados + entregas de quien ya no está matriculado (no se pierden).
            var ids = estudiantes.Select(e => e.id)
                .Union(entregas.Where(e => e.fk_Estudiante != null).Select(e => e.fk_Estudiante!.Value))
                .ToList();

            var nombres = _context.Usuario
                .Where(u => ids.Contains(u.id))
                .Select(u => new { u.id, u.nombre, u.email })
                .ToDictionary(u => u.id);

            var filas = ids.Select(id =>
            {
                var e = entregas.FirstOrDefault(x => x.fk_Estudiante == id);
                nombres.TryGetValue(id, out var u);

                return new TutorEntregaResponse
                {
                    entregaId = e?.id,
                    estudianteId = id,
                    estudiante = u?.nombre ?? u?.email ?? $"Estudiante #{id}",
                    email = u?.email ?? "",
                    fechaEntrega = e?.fecha_Entrega,
                    tarde = e?.fecha_Entrega != null && tarea.Fecha_Limite != null && e.fecha_Entrega > tarea.Fecha_Limite,
                    estado = EstadoEntrega(e),
                    calificacion = e?.calificacion,
                    feedback = e?.feedback,
                    enlace = EsEnlace(e?.archivo_Entrega_URL) ? e!.archivo_Entrega_URL : null,
                    archivos = e?.Archivos.Select(a => new TutorArchivoResponse
                    {
                        id = a.id,
                        nombre = a.nombre_Archivo ?? "archivo",
                        tipo = a.tipo_Archivo ?? ""
                    }).ToList() ?? new()
                };
            })
            // Primero lo que falta calificar, luego lo calificado, al final quien no entregó.
            .OrderBy(f => f.estado == EntregaTareaModel.EstadoEntregada ? 0 : f.estado == EntregaTareaModel.EstadoCalificada ? 1 : 2)
            .ThenBy(f => f.estudiante)
            .ToList();

            return new TutorEntregasResponse
            {
                curso = curso.nombre,
                tarea = MapTarea(tarea, curso.fechaInicio, estudiantesActivos, entregas),
                entregas = filas
            };
        }

        public (bool exitoso, string mensaje) Calificar(int entregaId, TutorCalificarRequest request, string tutorEmail)
        {
            var entrega = _context.EntregaTarea.FirstOrDefault(e => e.id == entregaId);

            if (entrega == null)
                return (false, "La entrega no existe.");

            if (entrega.fecha_Entrega == null)
                return (false, "No se puede calificar una tarea que el estudiante no ha entregado.");

            var corregida = entrega.calificacion != null;

            entrega.calificacion = Math.Round(request.calificacion, 2);
            entrega.feedback = string.IsNullOrWhiteSpace(request.feedback) ? null : request.feedback.Trim();
            entrega.estado = EntregaTareaModel.EstadoCalificada;

            _context.SaveChanges();

            _auditoria.Registrar("ENTREGA_TAREA", corregida ? "REGRADE" : "GRADE",
                $"Entrega {entrega.id} calificada con {entrega.calificacion:0.##}", tutorEmail, entrega.id);

            return (true, corregida ? "Calificación actualizada." : "Entrega calificada.");
        }

        public async Task<(ArchivoDescarga contenido, string nombre)?> AbrirArchivoEntrega(int archivoId, CancellationToken ct)
        {
            var archivo = _context.EntregaTareaArchivo.AsNoTracking().FirstOrDefault(a => a.id == archivoId && a.estado);
            if (archivo == null)
                return null;

            return await Abrir(archivo.archivo_URL, archivo.nombre_Archivo ?? "entrega", ct);
        }

        // ==================================================================
        // Material del curso
        // ==================================================================

        public List<TutorMaterialResponse> Material(int cursoId)
        {
            return _context.MaterialCurso
                .AsNoTracking()
                .Where(m => m.fk_Curso == cursoId && m.estado == true)
                .OrderBy(m => m.id)
                .AsEnumerable()
                .Select(MapMaterial)
                .ToList();
        }

        public async Task<(TutorMaterialResponse? material, string? error)> AgregarMaterial(
            int cursoId, string titulo, IFormFile? archivo, string? enlace, int autorId, string autorEmail)
        {
            titulo = titulo?.Trim() ?? "";

            if (titulo.Length == 0)
                return (null, "El título es obligatorio.");

            if (titulo.Length > 100)
                return (null, "El título admite como máximo 100 caracteres.");

            var tieneArchivo = archivo != null && archivo.Length > 0;
            var tieneEnlace = !string.IsNullOrWhiteSpace(enlace);

            if (tieneArchivo == tieneEnlace)
                return (null, "Suba un archivo o indique un enlace (solo uno de los dos).");

            string ubicacion, tipo;

            if (tieneEnlace)
            {
                if (!Uri.TryCreate(enlace!.Trim(), UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                    return (null, "El enlace debe ser una dirección web completa (https://…).");

                if (uri.AbsoluteUri.Length > 500)
                    return (null, "El enlace es demasiado largo (máx. 500 caracteres).");

                ubicacion = uri.AbsoluteUri;
                tipo = "Enlace";
            }
            else
            {
                // Mismas reglas que los adjuntos del blog: extensión, tamaño y firma real.
                var (valido, error) = await BlogArchivoValidador.Validar(archivo!);
                if (error != null)
                    return (null, error);

                try
                {
                    // Documentos al contenedor privado; imágenes al de imágenes (el servicio de
                    // Blob solo admite imágenes allí). En ambos casos se descargan por la API.
                    var subido = await _blob.SubirAsync(archivo!, ContenedorMaterial(TipoMaterial(valido!.Extension)), $"cursos/{cursoId}/material");
                    ubicacion = subido.BlobName;
                }
                catch (ArchivoInvalidoException ex)
                {
                    return (null, ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al subir material del curso {CursoId}.", cursoId);
                    return (null, "No se pudo guardar el archivo. Intente nuevamente.");
                }

                tipo = TipoMaterial(valido!.Extension);
            }

            var material = new MaterialCursoModel
            {
                fk_Curso = cursoId,
                titulo = titulo,
                materialUrl = ubicacion,
                tipo = tipo,
                estado = true,
                fk_Autor = autorId
            };

            _context.MaterialCurso.Add(material);
            _context.SaveChanges();

            _auditoria.Registrar("MATERIAL_CURSO", "CREATE", $"Material agregado al curso {cursoId}: {Recortar(titulo)}", autorEmail, material.id);

            return (MapMaterial(material), null);
        }

        /// <summary>Abre un material de tipo archivo (los enlaces no pasan por aquí).</summary>
        public async Task<(ArchivoDescarga contenido, string nombre)?> AbrirMaterial(int materialId, CancellationToken ct)
        {
            var material = _context.MaterialCurso.AsNoTracking().FirstOrDefault(m => m.id == materialId && m.estado == true);
            if (material == null || string.IsNullOrEmpty(material.materialUrl) || EsEnlace(material.materialUrl))
                return null;

            // El nombre de descarga: el título más la extensión real del archivo.
            var nombre = (material.titulo ?? "material") + Path.GetExtension(material.materialUrl);

            return await Abrir(material.materialUrl, nombre, ct, ContenedorMaterial(material.tipo));
        }

        /// <summary>
        /// True si un estudiante puede ver el contenido del curso: matrícula activa
        /// y curso activo (un curso inactivo no está disponible para los estudiantes).
        /// </summary>
        public bool PuedeVerComoEstudiante(int cursoId, int usuarioId) =>
            _context.Curso.Any(c => c.id == cursoId && c.estado) && EstaMatriculado(cursoId, usuarioId);

        /// <summary>True si el usuario tiene una matrícula activa en el curso.</summary>
        public bool EstaMatriculado(int cursoId, int usuarioId) =>
            _context.EstudianteCurso.Any(m => m.CursoId == cursoId && m.EstudianteId == usuarioId && m.Estado == MatriculaActiva);

        public async Task<bool> QuitarMaterial(int materialId, string usuarioEmail)
        {
            var material = _context.MaterialCurso.FirstOrDefault(m => m.id == materialId && m.estado == true);
            if (material == null)
                return false;

            material.estado = false;
            _context.SaveChanges();

            if (!EsEnlace(material.materialUrl) && !string.IsNullOrEmpty(material.materialUrl))
            {
                try
                {
                    await _blob.EliminarAsync(ContenedorMaterial(material.tipo), material.materialUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo borrar el material {MaterialId} de Blob Storage.", materialId);
                }
            }

            _auditoria.Registrar("MATERIAL_CURSO", "DISABLE", $"Material quitado: {Recortar(material.titulo ?? "")}", usuarioEmail, material.id);

            return true;
        }

        // ==================================================================
        // Ayudantes
        // ==================================================================

        private List<int> TareasActivasIds(int cursoId) =>
            _context.Tarea.Where(t => t.CursoId == cursoId && t.Estado != false).Select(t => t.ID).ToList();

        private List<EntregaTareaModel> EntregasDelCurso(List<int> tareaIds) =>
            _context.EntregaTarea.AsNoTracking().Where(e => e.fk_Tarea != null && tareaIds.Contains(e.fk_Tarea.Value)).ToList();

        private static TutorTareaResponse MapTarea(TareaModel t, DateTime inicioCurso, int estudiantes, List<EntregaTareaModel> entregas)
        {
            var propias = entregas.Where(e => e.fk_Tarea == t.ID).ToList();

            return new TutorTareaResponse
            {
                id = t.ID,
                cursoId = t.CursoId ?? 0,
                titulo = t.Titulo ?? "",
                descripcion = t.Descripcion ?? "",
                fechaInicio = t.Fecha_Inicio,
                fechaLimite = t.Fecha_Limite,
                semana = CursoModel.SemanaDe(inicioCurso, t.Fecha_Inicio),
                estado = t.Estado != false,
                entregas = propias.Count(e => e.fecha_Entrega != null),
                calificadas = propias.Count(e => e.calificacion != null),
                estudiantes = estudiantes
            };
        }

        private static string EstadoEntrega(EntregaTareaModel? e) =>
            e == null || e.fecha_Entrega == null ? "Sin entregar"
            : e.calificacion != null ? EntregaTareaModel.EstadoCalificada
            : EntregaTareaModel.EstadoEntregada;

        private static TutorMaterialResponse MapMaterial(MaterialCursoModel m) => new()
        {
            id = m.id,
            titulo = m.titulo ?? "",
            tipo = m.tipo ?? "",
            esEnlace = EsEnlace(m.materialUrl),
            url = EsEnlace(m.materialUrl) ? m.materialUrl : null
        };

        public static bool EsEnlace(string? valor) =>
            valor != null && (valor.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                              || valor.StartsWith("http://", StringComparison.OrdinalIgnoreCase));

        // MATERIAL_CURSO.Tipo es varchar(50): se guarda una etiqueta, no el MIME completo.
        public static string TipoMaterial(string extension) => extension switch
        {
            ".pdf" => "PDF",
            ".doc" or ".docx" => "Word",
            ".xlsx" => "Excel",
            ".pptx" => "PowerPoint",
            ".txt" or ".csv" => "Texto",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" => "Imagen",
            _ => "Archivo"
        };

        /// <summary>Contenedor donde vive un material según su tipo guardado.</summary>
        public static ContenedorBlob ContenedorMaterial(string? tipo) =>
            tipo == "Imagen" ? ContenedorBlob.Imagenes : ContenedorBlob.Documentos;

        private async Task<(ArchivoDescarga, string)?> Abrir(string blobName, string nombre, CancellationToken ct,
            ContenedorBlob contenedor = ContenedorBlob.Documentos)
        {
            try
            {
                return (await _blob.DescargarAsync(contenedor, blobName, ct), nombre);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("El archivo {Blob} no existe en Blob Storage.", blobName);
                return null;
            }
        }

        private static string Recortar(string texto) =>
            texto.Length > 120 ? texto[..120] + "…" : texto;
    }
}
