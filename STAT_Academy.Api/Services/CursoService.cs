using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Storage;
using STAT_Academy.DTOs.Curso;

namespace STAT_Academy.Api.Services
{
    /// <summary>
    /// Cursos y su portada (CURSO_IMAGEN en el contenedor público "imagenes").
    /// </summary>
    public class CursoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBlobStorageService _blob;
        private readonly AuditoriaService _auditoria;
        private readonly ILogger<CursoService> _logger;

        public CursoService(
            ApplicationDbContext context,
            IBlobStorageService blob,
            AuditoriaService auditoria,
            ILogger<CursoService> logger)
        {
            _context = context;
            _blob = blob;
            _auditoria = auditoria;
            _logger = logger;
        }

        // ==================================================================
        // Consultas
        // ==================================================================

        /// <param name="esAdmin">
        /// Admin ve todos los cursos. El catálogo público solo muestra los activos
        /// que todavía no han iniciado.
        /// </param>
        public List<CursoResponse> GetAll(bool esAdmin)
        {
            var hoy = CursoModel.HoyCostaRica;

            var cursos = _context.Curso
                .AsNoTracking()
                .Where(x => esAdmin || (x.estado && x.fechaInicio >= hoy))
                .OrderBy(x => x.nombre)
                .ToList();

            return MapLista(cursos);
        }

        public CursoModel? GetById(int id)
        {
            return _context.Curso.FirstOrDefault(x => x.id == id);
        }

        public CursoResponse? GetResponse(int id)
        {
            var curso = _context.Curso.AsNoTracking().FirstOrDefault(x => x.id == id);
            return curso == null ? null : MapLista([curso]).First();
        }

        /// <summary>
        /// El tutor asignado debe ser un usuario activo con rol Tutor (FK_Tipo_Usuario = 2);
        /// si no, el curso no aparecería en ningún panel de tutor.
        /// </summary>
        public string? ValidarTutor(int fkTutor)
        {
            var tutor = _context.Usuario
                .Where(u => u.id == fkTutor)
                .Select(u => new { u.fk_Tipo_Usuario, u.estado })
                .FirstOrDefault();

            if (tutor == null)
                return "El tutor seleccionado no existe. Elija otro tutor.";

            if (tutor.fk_Tipo_Usuario != 2)
                return "La persona seleccionada no tiene rol de tutor. Elija otro tutor.";

            if (!tutor.estado)
                return "El tutor seleccionado está inactivo. Elija otro tutor.";

            return null;
        }

        // ==================================================================
        // Altas y cambios
        // ==================================================================

        public CursoResponse Crear(CursoCreateRequest request)
        {
            CursoModel curso = new CursoModel();

            curso.fk_tutor = request.fk_tutor;
            curso.fk_creador = request.fk_creador;
            curso.nombre = request.nombre;
            curso.descripcion = request.descripcion;
            curso.precio = request.precio;
            curso.fechaInicio = request.fechaInicio;
            curso.fechaFin = request.fechaFin;
            curso.estado = true;

            _context.Curso.Add(curso);
            _context.SaveChanges();

            return GetResponse(curso.id)!;
        }

        public CursoResponse? Editar(int id, CursoCreateRequest request)
        {
            var curso = _context.Curso.FirstOrDefault(x => x.id == id);

            if (curso == null)
                return null;

            curso.fk_tutor = request.fk_tutor;
            curso.fk_creador = request.fk_creador;
            curso.nombre = request.nombre;
            curso.descripcion = request.descripcion;
            curso.precio = request.precio;
            curso.fechaInicio = request.fechaInicio;
            curso.fechaFin = request.fechaFin;

            _context.SaveChanges();

            return GetResponse(id);
        }

        public CursoResponse? Activar(int id) => CambiarEstado(id, true);

        public CursoResponse? Desactivar(int id) => CambiarEstado(id, false);

        private CursoResponse? CambiarEstado(int id, bool estado)
        {
            var curso = _context.Curso.FirstOrDefault(x => x.id == id);

            if (curso == null)
                return null;

            curso.estado = estado;

            _context.SaveChanges();

            return GetResponse(id);
        }

        // ==================================================================
        // Portada
        // ==================================================================

        /// <summary>
        /// Sube una nueva portada y reemplaza la anterior (que se desactiva y se borra de Blob Storage,
        /// porque el contenedor es público y su URL seguiría funcionando).
        /// </summary>
        public async Task<(CursoResponse? curso, string? error)> CambiarPortada(
            int cursoId, IFormFile? archivo, string usuarioEmail, CancellationToken ct)
        {
            if (!_context.Curso.Any(c => c.id == cursoId))
                return (null, "El curso no existe.");

            if (archivo == null || archivo.Length == 0)
                return (null, "Seleccione una imagen.");

            var (valido, error) = await BlogArchivoValidador.Validar(archivo);
            if (error != null)
                return (null, error);
            if (!valido!.EsImagen)
                return (null, $"'{valido.NombreOriginal}' no es una imagen (JPG, PNG, GIF o WEBP).");

            ArchivoSubido subido;
            try
            {
                subido = await _blob.SubirAsync(archivo, ContenedorBlob.Imagenes, $"cursos/{cursoId}", ct);
            }
            catch (ArchivoInvalidoException ex)
            {
                return (null, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir la portada del curso {CursoId}.", cursoId);
                return (null, "No se pudo guardar la portada. Intente nuevamente.");
            }

            var anteriores = _context.CursoImagen.Where(i => i.fk_curso == cursoId && i.estado).ToList();

            try
            {
                foreach (var anterior in anteriores)
                {
                    anterior.estado = false;
                    anterior.es_principal = false;
                }

                _context.CursoImagen.Add(new CursoImagenModel
                {
                    fk_curso = cursoId,
                    imagen_url = subido.BlobName,
                    nombre_archivo = subido.NombreOriginal,
                    es_principal = true,
                    estado = true,
                    fecha_creacion = DateTime.UtcNow
                });

                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar la portada del curso {CursoId}.", cursoId);
                await BorrarBlobs([subido.BlobName]);
                return (null, "No se pudo guardar la portada. Intente nuevamente.");
            }

            await BorrarBlobs(anteriores.Select(a => a.imagen_url));
            _auditoria.Registrar("CURSO", "UPLOAD", $"Portada: {subido.NombreOriginal}", usuarioEmail, cursoId);

            return (GetResponse(cursoId), null);
        }

        /// <returns>False si el curso no tenía portada.</returns>
        public async Task<bool> QuitarPortada(int cursoId, string usuarioEmail)
        {
            var imagenes = _context.CursoImagen.Where(i => i.fk_curso == cursoId && i.estado).ToList();
            if (imagenes.Count == 0)
                return false;

            foreach (var imagen in imagenes)
            {
                imagen.estado = false;
                imagen.es_principal = false;
            }

            _context.SaveChanges();
            await BorrarBlobs(imagenes.Select(i => i.imagen_url));

            _auditoria.Registrar("CURSO", "REMOVE_IMAGE", "Portada quitada", usuarioEmail, cursoId);
            return true;
        }

        // ==================================================================
        // Ayudantes
        // ==================================================================

        private List<CursoResponse> MapLista(List<CursoModel> cursos)
        {
            var ids = cursos.Select(c => c.id).ToList();
            var tutorIds = cursos.Select(c => c.fk_tutor).Distinct().ToList();

            var tutores = _context.Usuario
                .AsNoTracking()
                .Where(u => tutorIds.Contains(u.id))
                .Select(u => new { u.id, u.nombre })
                .ToDictionary(u => u.id, u => u.nombre);

            var portadas = Portadas(ids);

            return cursos.Select(c => new CursoResponse
            {
                id = c.id,
                fk_tutor = c.fk_tutor,
                fk_creador = c.fk_creador,
                nombre = c.nombre,
                descripcion = c.descripcion,
                precio = c.precio,
                duracionSemanas = c.duracionSemanas,
                estado = c.estado,
                fechaInicio = c.fechaInicio,
                fechaFin = c.fechaFin,
                tutorNombre = tutores.GetValueOrDefault(c.fk_tutor),
                imagenPortada = portadas.TryGetValue(c.id, out var blob) ? _blob.ObtenerUrlPublica(blob) : null,
                inscripcionAbierta = c.inscripcionAbierta
            }).ToList();
        }

        /// <summary>Blob de la portada de cada curso (id → nombre del blob).</summary>
        private Dictionary<int, string> Portadas(List<int> cursoIds)
        {
            if (cursoIds.Count == 0)
                return [];

            try
            {
                return _context.CursoImagen
                    .AsNoTracking()
                    .Where(i => cursoIds.Contains(i.fk_curso) && i.estado && i.es_principal)
                    .OrderByDescending(i => i.id)
                    .Select(i => new { i.fk_curso, i.imagen_url })
                    .AsEnumerable()
                    .DistinctBy(i => i.fk_curso)
                    .ToDictionary(i => i.fk_curso, i => i.imagen_url);
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                // 208 = "Invalid object name": falta aplicar 2026-11-14_curso_imagen.sql.
                // El catálogo sigue funcionando, solo sin portadas.
                _logger.LogWarning("No existe la tabla CURSO_IMAGEN; los cursos se muestran sin portada.");
                return [];
            }
        }

        private async Task BorrarBlobs(IEnumerable<string> blobs)
        {
            foreach (var blob in blobs)
            {
                try
                {
                    await _blob.EliminarAsync(ContenedorBlob.Imagenes, blob);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo borrar {Blob} de Blob Storage.", blob);
                }
            }
        }
    }
}
