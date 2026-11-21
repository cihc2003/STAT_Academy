using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Storage;
using STAT_Academy.DTOs.Aula;
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
        private readonly NotaFinalService _notaFinal;
        private readonly CorreoService _correo;
        private readonly ILogger<TutorService> _logger;

        public TutorService(
            ApplicationDbContext context,
            AuditoriaService auditoria,
            IBlobStorageService blob,
            NotaFinalService notaFinal,
            CorreoService correo,
            ILogger<TutorService> logger)
        {
            _context = context;
            _auditoria = auditoria;
            _blob = blob;
            _notaFinal = notaFinal;
            _correo = correo;
            _logger = logger;
        }

        // ==================================================================
        // Permisos
        // ==================================================================

        /// <summary>Tutor asignado al curso, o null si el curso no existe.</summary>
        public async Task<(bool existe, int tutorId)> TutorDelCursoAsync(int cursoId)
        {
            var tutor = await _context.Curso
                .Where(c => c.id == cursoId)
                .Select(c => (int?)c.fk_tutor)
                .FirstOrDefaultAsync();

            return (tutor != null, tutor ?? 0);
        }

        public Task<int?> CursoDeTareaAsync(int tareaId) =>
            _context.Tarea.Where(t => t.ID == tareaId).Select(t => t.CursoId).FirstOrDefaultAsync();

        public Task<int?> CursoDeEntregaAsync(int entregaId) =>
            (from e in _context.EntregaTarea
             join t in _context.Tarea on e.fk_Tarea equals t.ID
             where e.id == entregaId
             select t.CursoId).FirstOrDefaultAsync();

        public Task<int?> CursoDeArchivoEntregaAsync(int archivoId) =>
            (from a in _context.EntregaTareaArchivo
             join e in _context.EntregaTarea on a.fk_Entrega_Tarea equals e.id
             join t in _context.Tarea on e.fk_Tarea equals t.ID
             where a.id == archivoId && a.estado
             select t.CursoId).FirstOrDefaultAsync();

        public Task<int?> CursoDeMaterialAsync(int materialId) =>
            _context.MaterialCurso.Where(m => m.id == materialId).Select(m => m.fk_Curso).FirstOrDefaultAsync();

        // ==================================================================
        // Cursos asignados
        // ==================================================================

        /// <param name="tutorId">null = todos los cursos (Admin).</param>
        public async Task<List<TutorCursoResponse>> CursosAsignadosAsync(int? tutorId)
        {
            var cursos = await _context.Curso
                .AsNoTracking()
                .Where(c => tutorId == null || c.fk_tutor == tutorId)
                .OrderByDescending(c => c.estado)
                .ThenBy(c => c.nombre)
                .ToListAsync();

            // Un DbContext no admite consultas en paralelo: los resúmenes se arman uno a uno.
            var resumenes = new List<TutorCursoResponse>(cursos.Count);
            foreach (var curso in cursos)
                resumenes.Add(await ResumenCursoAsync(curso));

            return resumenes;
        }

        public async Task<TutorCursoResponse?> CursoAsync(int cursoId)
        {
            var curso = await _context.Curso.AsNoTracking().FirstOrDefaultAsync(c => c.id == cursoId);
            return curso == null ? null : await ResumenCursoAsync(curso);
        }

        private async Task<TutorCursoResponse> ResumenCursoAsync(CursoModel c)
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
                estudiantes = await _context.EstudianteCurso.CountAsync(m => m.CursoId == c.id && m.Estado == MatriculaActiva),
                tareas = await tareasActivas.CountAsync(),
                entregasPorCalificar = await _context.EntregaTarea.CountAsync(e =>
                    tareasActivas.Any(t => t.ID == e.fk_Tarea) && e.calificacion == null && e.fecha_Entrega != null),
                material = await _context.MaterialCurso.CountAsync(m => m.fk_Curso == c.id && m.estado == true)
            };
        }

        // ==================================================================
        // Estudiantes
        // ==================================================================

        public async Task<List<TutorEstudianteResponse>> EstudiantesAsync(int cursoId)
        {
            var matriculas = await (from m in _context.EstudianteCurso
                              join u in _context.Usuario on m.EstudianteId equals u.id
                              where m.CursoId == cursoId
                              orderby u.nombre
                              select new { m, u.nombre, u.email })
                             .AsNoTracking()
                             .ToListAsync();

            var curso = await _context.Curso.AsNoTracking().FirstAsync(c => c.id == cursoId);
            var progreso = CursoModel.ProgresoDe(curso.fechaInicio, curso.fechaFin);

            var tareas = await TareasActivasAsync(cursoId);
            var tareaIds = tareas.Select(t => t.ID).ToList();
            var entregas = await EntregasDelCursoAsync(tareaIds);
            var ajustes = await _notaFinal.AjustesAsync(matriculas.Select(x => x.m.IdMatricula).ToList());

            return matriculas.Select(x =>
            {
                var propias = entregas.Where(e => e.fk_Estudiante == x.m.EstudianteId).ToList();
                var notas = propias.Where(e => e.calificacion != null).Select(e => e.calificacion!.Value).ToList();

                return new TutorEstudianteResponse
                {
                    matriculaId = x.m.IdMatricula,
                    estudianteId = x.m.EstudianteId,
                    nombre = x.nombre ?? x.email ?? "Estudiante sin nombre",
                    email = x.email ?? "",
                    fechaMatricula = x.m.Fecha_Matricula,
                    estadoMatricula = x.m.Estado ?? "",
                    // Tiempo transcurrido del curso (la columna Progreso nunca se actualiza).
                    progreso = progreso,
                    tareasEntregadas = propias.Count(e => e.fecha_Entrega != null),
                    tareasTotales = tareaIds.Count,
                    promedio = notas.Count > 0 ? Math.Round(notas.Average(), 2) : null,
                    notaFinal = NotaFinalCalculo.Calcular(tareas, propias, ajustes.GetValueOrDefault(x.m.IdMatricula))
                };
            }).ToList();
        }

        // ==================================================================
        // Calificaciones (tabla estudiantes × tareas)
        // ==================================================================

        public async Task<TutorCalificacionesResponse> CalificacionesAsync(int cursoId)
        {
            var tareas = await TareasActivasAsync(cursoId);

            var estudiantes = await (from m in _context.EstudianteCurso
                               join u in _context.Usuario on m.EstudianteId equals u.id
                               where m.CursoId == cursoId && m.Estado == MatriculaActiva
                               orderby u.nombre
                               select new { u.id, u.nombre, u.email, matriculaId = m.IdMatricula })
                              .AsNoTracking()
                              .ToListAsync();

            var entregas = await EntregasDelCursoAsync(tareas.Select(t => t.ID).ToList());
            var ajustes = await _notaFinal.AjustesAsync(estudiantes.Select(e => e.matriculaId).ToList());

            return new TutorCalificacionesResponse
            {
                ajusteDisponible = _notaFinal.Disponible,
                tareas = tareas.Select(t => new TutorTareaColumna
                {
                    id = t.ID,
                    titulo = t.Titulo ?? "Tarea sin título",
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
                            calificacion = e?.calificacion,
                            cuentaComoCero = e?.fecha_Entrega == null && NotaFinalCalculo.Vencida(t)
                        };
                    }).ToList();

                    var notas = celdas.Where(c => c.calificacion != null).Select(c => c.calificacion!.Value).ToList();

                    return new TutorFilaCalificaciones
                    {
                        estudianteId = est.id,
                        nombre = est.nombre ?? est.email ?? "Estudiante sin nombre",
                        email = est.email ?? "",
                        notas = celdas,
                        promedio = notas.Count > 0 ? Math.Round(notas.Average(), 2) : null,
                        notaFinal = NotaFinalCalculo.Calcular(tareas,
                            entregas.Where(x => x.fk_Estudiante == est.id), ajustes.GetValueOrDefault(est.matriculaId))
                    };
                }).ToList()
            };
        }

        /// <summary>Ajusta (o devuelve a la calculada, con nota = null) la nota final de un estudiante.</summary>
        public async Task<(bool exitoso, string mensaje)> AjustarNotaFinalAsync(
            int cursoId, int estudianteId, TutorNotaFinalRequest request, int tutorId, string tutorEmail)
        {
            var matricula = await (from m in _context.EstudianteCurso
                             join u in _context.Usuario on m.EstudianteId equals u.id
                             where m.CursoId == cursoId && m.EstudianteId == estudianteId && m.Estado == MatriculaActiva
                             select new { m.IdMatricula, u.nombre, u.email })
                            .AsNoTracking()
                            .FirstOrDefaultAsync();

            if (matricula == null)
                return (false, "El estudiante no tiene una matrícula activa en este curso.");

            return await _notaFinal.GuardarAsync(matricula.IdMatricula, request.nota, request.comentario,
                tutorId, tutorEmail, matricula.nombre ?? matricula.email ?? "estudiante");
        }

        // ==================================================================
        // Tareas
        // ==================================================================

        public async Task<List<TutorTareaResponse>> TareasAsync(int cursoId)
        {
            var inicioCurso = await _context.Curso.Where(c => c.id == cursoId).Select(c => c.fechaInicio).FirstAsync();
            var estudiantes = await _context.EstudianteCurso.CountAsync(m => m.CursoId == cursoId && m.Estado == MatriculaActiva);

            var tareas = await _context.Tarea
                .AsNoTracking()
                .Where(t => t.CursoId == cursoId)
                .OrderBy(t => t.Fecha_Inicio)
                .ToListAsync();

            var entregas = await EntregasDelCursoAsync(tareas.Select(t => t.ID).ToList());

            return tareas.Select(t => MapTarea(t, inicioCurso, estudiantes, entregas)).ToList();
        }

        public async Task<TutorTareaResponse> CrearTareaAsync(int cursoId, TutorTareaRequest request, int autorId, string autorEmail)
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
            await _context.SaveChangesAsync();

            await _auditoria.RegistrarAsync("TAREA", "CREATE", $"Tarea creada en curso {cursoId}: {Recortar(tarea.Titulo)}", autorEmail, tarea.ID);

            return (await TareasAsync(cursoId)).First(t => t.id == tarea.ID);
        }

        public async Task<TutorTareaResponse?> EditarTareaAsync(int tareaId, TutorTareaRequest request, string usuarioEmail)
        {
            var tarea = await _context.Tarea.FirstOrDefaultAsync(t => t.ID == tareaId);
            if (tarea == null || tarea.CursoId == null)
                return null;

            tarea.Titulo = request.titulo.Trim();
            tarea.Descripcion = request.descripcion?.Trim() ?? "";
            tarea.Fecha_Inicio = request.fechaInicio;
            tarea.Fecha_Limite = request.fechaLimite;

            await _context.SaveChangesAsync();

            await _auditoria.RegistrarAsync("TAREA", "UPDATE", $"Tarea actualizada: {Recortar(tarea.Titulo)}", usuarioEmail, tarea.ID);

            return (await TareasAsync(tarea.CursoId.Value)).First(t => t.id == tareaId);
        }

        public async Task<bool> CambiarEstadoTareaAsync(int tareaId, bool activa, string usuarioEmail)
        {
            var tarea = await _context.Tarea.FirstOrDefaultAsync(t => t.ID == tareaId);
            if (tarea == null)
                return false;

            tarea.Estado = activa;
            await _context.SaveChangesAsync();

            await _auditoria.RegistrarAsync("TAREA", activa ? "ENABLE" : "DISABLE",
                $"Tarea {(activa ? "publicada" : "ocultada")}: {Recortar(tarea.Titulo ?? "")}", usuarioEmail, tarea.ID);

            return true;
        }

        // ==================================================================
        // Entregas y calificación
        // ==================================================================

        public async Task<TutorEntregasResponse?> EntregasAsync(int tareaId)
        {
            var tarea = await _context.Tarea.AsNoTracking().FirstOrDefaultAsync(t => t.ID == tareaId);
            if (tarea?.CursoId == null)
                return null;

            var cursoId = tarea.CursoId.Value;
            var curso = await _context.Curso.AsNoTracking().FirstAsync(c => c.id == cursoId);

            var estudiantes = await (from m in _context.EstudianteCurso
                               join u in _context.Usuario on m.EstudianteId equals u.id
                               where m.CursoId == cursoId && m.Estado == MatriculaActiva
                               orderby u.nombre
                               select new { u.id, u.nombre, u.email })
                              .AsNoTracking()
                              .ToListAsync();

            var entregas = await _context.EntregaTarea
                .AsNoTracking()
                .Include(e => e.Archivos.Where(a => a.estado))
                .Where(e => e.fk_Tarea == tareaId)
                .ToListAsync();

            var estudiantesActivos = estudiantes.Count;

            // Estudiantes matriculados + entregas de quien ya no está matriculado (no se pierden).
            var ids = estudiantes.Select(e => e.id)
                .Union(entregas.Where(e => e.fk_Estudiante != null).Select(e => e.fk_Estudiante!.Value))
                .ToList();

            var nombres = await _context.Usuario
                .Where(u => ids.Contains(u.id))
                .Select(u => new { u.id, u.nombre, u.email })
                .ToDictionaryAsync(u => u.id);

            var filas = ids.Select(id =>
            {
                var e = entregas.FirstOrDefault(x => x.fk_Estudiante == id);
                nombres.TryGetValue(id, out var u);

                return new TutorEntregaResponse
                {
                    entregaId = e?.id,
                    estudianteId = id,
                    estudiante = u?.nombre ?? u?.email ?? "Estudiante sin nombre",
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

        public async Task<(bool exitoso, string mensaje)> CalificarAsync(int entregaId, TutorCalificarRequest request, string tutorEmail)
        {
            var entrega = await _context.EntregaTarea.FirstOrDefaultAsync(e => e.id == entregaId);

            if (entrega == null)
                return (false, "La entrega no existe.");

            if (entrega.fecha_Entrega == null)
                return (false, "No se puede calificar una tarea que el estudiante no ha entregado.");

            var corregida = entrega.calificacion != null;

            entrega.calificacion = Math.Round(request.calificacion, 2);
            entrega.feedback = string.IsNullOrWhiteSpace(request.feedback) ? null : request.feedback.Trim();
            entrega.estado = EntregaTareaModel.EstadoCalificada;

            await _context.SaveChangesAsync();

            await _auditoria.RegistrarAsync("ENTREGA_TAREA", corregida ? "REGRADE" : "GRADE",
                $"Entrega {entrega.id} calificada con {entrega.calificacion:0.##}", tutorEmail, entrega.id);

            await AvisarEstudianteAsync(entrega, corregida);

            return (true, corregida ? "Calificación actualizada." : "Entrega calificada.");
        }

        public async Task<(ArchivoDescarga contenido, string nombre)?> AbrirArchivoEntrega(int archivoId, CancellationToken ct)
        {
            var archivo = await _context.EntregaTareaArchivo.AsNoTracking().FirstOrDefaultAsync(a => a.id == archivoId && a.estado, ct);
            if (archivo == null)
                return null;

            return await Abrir(archivo.archivo_URL, archivo.nombre_Archivo ?? "entrega", ct);
        }

        // ==================================================================
        // Material del curso
        // ==================================================================

        public async Task<List<TutorMaterialResponse>> MaterialAsync(int cursoId) =>
            (await _context.MaterialCurso
                .AsNoTracking()
                .Where(m => m.fk_Curso == cursoId && m.estado == true)
                .OrderBy(m => m.id)
                .ToListAsync())
            .Select(MapMaterial)
            .ToList();

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
            await _context.SaveChangesAsync();

            await _auditoria.RegistrarAsync("MATERIAL_CURSO", "CREATE", $"Material agregado al curso {cursoId}: {Recortar(titulo)}", autorEmail, material.id);

            return (MapMaterial(material), null);
        }

        /// <summary>Abre un material de tipo archivo (los enlaces no pasan por aquí).</summary>
        public async Task<(ArchivoDescarga contenido, string nombre)?> AbrirMaterial(int materialId, CancellationToken ct)
        {
            var material = await _context.MaterialCurso.AsNoTracking().FirstOrDefaultAsync(m => m.id == materialId && m.estado == true, ct);
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
        public async Task<bool> PuedeVerComoEstudianteAsync(int cursoId, int usuarioId) =>
            await _context.Curso.AnyAsync(c => c.id == cursoId && c.estado)
            && await EstaMatriculadoAsync(cursoId, usuarioId);

        /// <summary>True si el usuario tiene una matrícula activa en el curso.</summary>
        public Task<bool> EstaMatriculadoAsync(int cursoId, int usuarioId) =>
            _context.EstudianteCurso.AnyAsync(m => m.CursoId == cursoId && m.EstudianteId == usuarioId && m.Estado == MatriculaActiva);

        public async Task<bool> QuitarMaterial(int materialId, string usuarioEmail)
        {
            var material = await _context.MaterialCurso.FirstOrDefaultAsync(m => m.id == materialId && m.estado == true);
            if (material == null)
                return false;

            material.estado = false;
            await _context.SaveChangesAsync();

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

            await _auditoria.RegistrarAsync("MATERIAL_CURSO", "DISABLE", $"Material quitado: {Recortar(material.titulo ?? "")}", usuarioEmail, material.id);

            return true;
        }

        // ==================================================================
        // Ayudantes
        // ==================================================================

        private Task<List<TareaModel>> TareasActivasAsync(int cursoId) =>
            _context.Tarea
                .AsNoTracking()
                .Where(t => t.CursoId == cursoId && t.Estado != false)
                .OrderBy(t => t.Fecha_Limite)
                .ToListAsync();

        /// <summary>
        /// Correo al estudiante con su nota. Es complementario: se arma con los datos ya leídos
        /// y se envía en segundo plano para no demorar la respuesta (si falla, solo se registra).
        /// </summary>
        private async Task AvisarEstudianteAsync(EntregaTareaModel entrega, bool corregida)
        {
            try
            {
                var estudianteId = entrega.fk_Estudiante ?? 0;
                var datos = await (from t in _context.Tarea
                             join c in _context.Curso on t.CursoId equals c.id
                             from u in _context.Usuario.Where(u => u.id == estudianteId)
                             where t.ID == entrega.fk_Tarea
                             select new { tarea = t.Titulo, t.ID, curso = c.nombre, u.email, u.nombre })
                            .AsNoTracking()
                            .FirstOrDefaultAsync();

                if (datos == null || string.IsNullOrWhiteSpace(datos.email) || entrega.calificacion == null)
                    return;

                var nota = entrega.calificacion.Value;
                CorreoService.EnSegundoPlano(_logger, $"la nota de la entrega {entrega.id}", () =>
                    _correo.EnviarTareaCalificada(datos.email, datos.nombre ?? "", datos.curso,
                        datos.tarea ?? "Tarea", datos.ID, nota, corregida));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo preparar el aviso de la nota de la entrega {EntregaId}.", entrega.id);
            }
        }

        private Task<List<EntregaTareaModel>> EntregasDelCursoAsync(List<int> tareaIds) =>
            _context.EntregaTarea.AsNoTracking().Where(e => e.fk_Tarea != null && tareaIds.Contains(e.fk_Tarea.Value)).ToListAsync();

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
