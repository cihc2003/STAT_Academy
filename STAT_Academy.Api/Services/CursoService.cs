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
        public async Task<List<CursoResponse>> GetAllAsync(bool esAdmin)
        {
            var hoy = CursoModel.HoyCostaRica;

            var cursos = await _context.Curso
                .AsNoTracking()
                .Where(x => esAdmin || (x.estado && x.fechaInicio >= hoy))
                .OrderBy(x => x.nombre)
                .ToListAsync();

            return await MapListaAsync(cursos);
        }

        public Task<CursoModel?> GetByIdAsync(int id) =>
            _context.Curso.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);

        public async Task<CursoResponse?> GetResponseAsync(int id)
        {
            var curso = await _context.Curso.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
            return curso == null ? null : (await MapListaAsync([curso])).First();
        }

        /// <summary>
        /// El tutor asignado debe ser un usuario activo con rol Tutor (FK_Tipo_Usuario = 2);
        /// si no, el curso no aparecería en ningún panel de tutor.
        /// </summary>
        public async Task<string?> ValidarTutorAsync(int fkTutor)
        {
            var tutor = await _context.Usuario
                .Where(u => u.id == fkTutor)
                .Select(u => new { u.fk_Tipo_Usuario, u.estado })
                .FirstOrDefaultAsync();

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

        /// <param name="creadorId">Admin que crea el curso (sale del token, no del cuerpo).</param>
        public async Task<CursoResponse> CrearAsync(CursoCreateRequest request, int creadorId)
        {
            CursoModel curso = new CursoModel();

            curso.fk_tutor = request.fk_tutor;
            curso.fk_creador = creadorId;
            curso.nombre = request.nombre;
            curso.descripcion = request.descripcion;
            curso.precio = request.precio;
            curso.fechaInicio = request.fechaInicio;
            curso.fechaFin = request.fechaFin;
            curso.tipo = CursoCreateRequest.Limpiar(request.tipo);
            curso.prerrequisitos = CursoCreateRequest.Limpiar(request.prerrequisitos);
            curso.estado = true;

            _context.Curso.Add(curso);
            await _context.SaveChangesAsync();

            return (await GetResponseAsync(curso.id))!;
        }

        public async Task<CursoResponse?> EditarAsync(int id, CursoCreateRequest request)
        {
            var curso = await _context.Curso.FirstOrDefaultAsync(x => x.id == id);

            if (curso == null)
                return null;

            // El creador no cambia al editar.
            curso.fk_tutor = request.fk_tutor;
            curso.nombre = request.nombre;
            curso.descripcion = request.descripcion;
            curso.precio = request.precio;
            curso.fechaInicio = request.fechaInicio;
            curso.fechaFin = request.fechaFin;
            curso.tipo = CursoCreateRequest.Limpiar(request.tipo);
            curso.prerrequisitos = CursoCreateRequest.Limpiar(request.prerrequisitos);

            await _context.SaveChangesAsync();

            return await GetResponseAsync(id);
        }

        public Task<CursoResponse?> ActivarAsync(int id) => CambiarEstadoAsync(id, true);

        public Task<CursoResponse?> DesactivarAsync(int id) => CambiarEstadoAsync(id, false);

        private async Task<CursoResponse?> CambiarEstadoAsync(int id, bool estado)
        {
            var curso = await _context.Curso.FirstOrDefaultAsync(x => x.id == id);

            if (curso == null)
                return null;

            curso.estado = estado;

            await _context.SaveChangesAsync();

            return await GetResponseAsync(id);
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
            if (!await _context.Curso.AnyAsync(c => c.id == cursoId, ct))
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

            var anteriores = await _context.CursoImagen.Where(i => i.fk_curso == cursoId && i.estado).ToListAsync(ct);

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

                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar la portada del curso {CursoId}.", cursoId);
                await BorrarBlobs([subido.BlobName]);
                return (null, "No se pudo guardar la portada. Intente nuevamente.");
            }

            await BorrarBlobs(anteriores.Select(a => a.imagen_url));
            await _auditoria.RegistrarAsync("CURSO", "UPLOAD", $"Portada: {subido.NombreOriginal}", usuarioEmail, cursoId);

            return (await GetResponseAsync(cursoId), null);
        }

        /// <returns>False si el curso no tenía portada.</returns>
        public async Task<bool> QuitarPortada(int cursoId, string usuarioEmail)
        {
            var imagenes = await _context.CursoImagen.Where(i => i.fk_curso == cursoId && i.estado).ToListAsync();
            if (imagenes.Count == 0)
                return false;

            foreach (var imagen in imagenes)
            {
                imagen.estado = false;
                imagen.es_principal = false;
            }

            await _context.SaveChangesAsync();
            await BorrarBlobs(imagenes.Select(i => i.imagen_url));

            await _auditoria.RegistrarAsync("CURSO", "REMOVE_IMAGE", "Portada quitada", usuarioEmail, cursoId);
            return true;
        }

        // ==================================================================
        // Ayudantes
        // ==================================================================

        private async Task<List<CursoResponse>> MapListaAsync(List<CursoModel> cursos)
        {
            var ids = cursos.Select(c => c.id).ToList();
            var tutorIds = cursos.Select(c => c.fk_tutor).Distinct().ToList();

            var tutores = await _context.Usuario
                .AsNoTracking()
                .Where(u => tutorIds.Contains(u.id))
                .Select(u => new { u.id, u.nombre })
                .ToDictionaryAsync(u => u.id, u => u.nombre);

            var portadas = await PortadasAsync(ids);

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
                tipo = c.tipo,
                prerrequisitos = c.prerrequisitos,
                tutorNombre = tutores.GetValueOrDefault(c.fk_tutor),
                imagenPortada = portadas.TryGetValue(c.id, out var blob) ? _blob.ObtenerUrlPublica(blob) : null,
                inscripcionAbierta = c.inscripcionAbierta
            }).ToList();
        }

        /// <summary>Blob de la portada de cada curso (id → nombre del blob).</summary>
        private async Task<Dictionary<int, string>> PortadasAsync(List<int> cursoIds)
        {
            if (cursoIds.Count == 0)
                return [];

            try
            {
                var filas = await _context.CursoImagen
                    .AsNoTracking()
                    .Where(i => cursoIds.Contains(i.fk_curso) && i.estado && i.es_principal)
                    .OrderByDescending(i => i.id)
                    .Select(i => new { i.fk_curso, i.imagen_url })
                    .ToListAsync();

                return filas
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
