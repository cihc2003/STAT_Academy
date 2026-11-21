using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Storage;
using STAT_Academy.DTOs.Aula;

namespace STAT_Academy.Api.Services
{
    /// <summary>
    /// Documentos y enlaces que el tutor adjunta a una tarea (tabla TAREA_ARCHIVO).
    /// Mismas reglas que el material del curso (extensión, tamaño y firma real del archivo).
    /// Si la tabla todavía no existe (falta 2026-11-15_tarea_archivo.sql), las lecturas
    /// devuelven una lista vacía y <see cref="Disponible"/> queda en false.
    /// Los permisos los validan los controladores (tutor del curso / estudiante matriculado).
    /// </summary>
    public class TareaAdjuntoService
    {
        public const int MaxAdjuntosPorTarea = 5;

        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly IBlobStorageService _blob;
        private readonly ILogger<TareaAdjuntoService> _logger;

        public TareaAdjuntoService(
            ApplicationDbContext context,
            AuditoriaService auditoria,
            IBlobStorageService blob,
            ILogger<TareaAdjuntoService> logger)
        {
            _context = context;
            _auditoria = auditoria;
            _blob = blob;
            _logger = logger;
        }

        /// <summary>False si la tabla TAREA_ARCHIVO no existe (se sabe después de la primera lectura).</summary>
        public bool Disponible { get; private set; } = true;

        public async Task<List<TareaAdjuntoResponse>> ListarAsync(int tareaId)
        {
            try
            {
                return (await _context.TareaArchivo
                        .AsNoTracking()
                        .Where(a => a.fk_Tarea == tareaId && a.estado)
                        .OrderBy(a => a.id)
                        .ToListAsync())
                    .Select(Map)
                    .ToList();
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                // 208 = "Invalid object name": falta aplicar 2026-11-15_tarea_archivo.sql.
                Disponible = false;
                _logger.LogWarning("No existe la tabla TAREA_ARCHIVO; las tareas se muestran sin adjuntos.");
                return [];
            }
        }

        /// <summary>Curso del adjunto activo, o null si no existe (o falta la tabla).</summary>
        public async Task<int?> CursoDeAdjuntoAsync(int adjuntoId)
        {
            try
            {
                return await (from a in _context.TareaArchivo
                              join t in _context.Tarea on a.fk_Tarea equals t.ID
                              where a.id == adjuntoId && a.estado
                              select t.CursoId).FirstOrDefaultAsync();
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                return null;
            }
        }

        /// <summary>Tarea del adjunto, solo si la tarea está visible para estudiantes.</summary>
        public async Task<bool> TareaActivaAsync(int adjuntoId)
        {
            try
            {
                return await (from a in _context.TareaArchivo
                              join t in _context.Tarea on a.fk_Tarea equals t.ID
                              where a.id == adjuntoId && a.estado && t.Estado != false
                              select a.id).AnyAsync();
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                return false;
            }
        }

        /// <summary>Agrega un archivo o un enlace (uno de los dos). El título es opcional para archivos.</summary>
        public async Task<(TareaAdjuntoResponse? adjunto, string? error)> Agregar(
            int tareaId, string? titulo, IFormFile? archivo, string? enlace, int autorId, string autorEmail)
        {
            titulo = titulo?.Trim() ?? "";

            var tieneArchivo = archivo is { Length: > 0 };
            var tieneEnlace = !string.IsNullOrWhiteSpace(enlace);

            if (tieneArchivo == tieneEnlace)
                return (null, "Suba un archivo o indique un enlace (solo uno de los dos).");

            if (titulo.Length > 255)
                return (null, "El título admite como máximo 255 caracteres.");

            int actuales;
            try
            {
                actuales = await _context.TareaArchivo.CountAsync(a => a.fk_Tarea == tareaId && a.estado);
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                Disponible = false;
                return (null, "Los documentos de las tareas todavía no están habilitados en la base de datos.");
            }

            if (actuales >= MaxAdjuntosPorTarea)
                return (null, $"Una tarea admite como máximo {MaxAdjuntosPorTarea} documentos o enlaces.");

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
                if (titulo.Length == 0)
                    titulo = uri.Host;
            }
            else
            {
                var (valido, error) = await BlogArchivoValidador.Validar(archivo!);
                if (error != null)
                    return (null, error);

                tipo = TutorService.TipoMaterial(valido!.Extension);
                try
                {
                    // Mismo criterio que el material: documentos al contenedor privado, imágenes al de imágenes.
                    var subido = await _blob.SubirAsync(archivo!, TutorService.ContenedorMaterial(tipo), $"tareas/{tareaId}");
                    ubicacion = subido.BlobName;
                }
                catch (ArchivoInvalidoException ex)
                {
                    return (null, ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al subir un adjunto de la tarea {TareaId}.", tareaId);
                    return (null, "No se pudo guardar el archivo. Intente nuevamente.");
                }

                if (titulo.Length == 0)
                    titulo = valido.NombreOriginal;
            }

            var adjunto = new TareaArchivoModel
            {
                fk_Tarea = tareaId,
                titulo = titulo,
                ubicacion = ubicacion,
                tipo = tipo,
                estado = true,
                fk_Autor = autorId,
                fecha_Creacion = DateTime.UtcNow
            };

            try
            {
                _context.TareaArchivo.Add(adjunto);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "No se registró el adjunto de la tarea {TareaId}.", tareaId);
                if (!TutorService.EsEnlace(ubicacion))
                    await BorrarBlob(tipo, ubicacion);
                return (null, "No se pudo guardar el documento. Intente nuevamente.");
            }

            await _auditoria.RegistrarAsync("TAREA_ARCHIVO", "CREATE", $"Documento agregado a la tarea {tareaId}: {Recortar(titulo)}", autorEmail, adjunto.id);

            return (Map(adjunto), null);
        }

        public async Task<int?> Quitar(int adjuntoId, string usuarioEmail)
        {
            var adjunto = await _context.TareaArchivo.FirstOrDefaultAsync(a => a.id == adjuntoId && a.estado);
            if (adjunto == null)
                return null;

            adjunto.estado = false;
            await _context.SaveChangesAsync();

            if (!TutorService.EsEnlace(adjunto.ubicacion))
                await BorrarBlob(adjunto.tipo, adjunto.ubicacion);

            await _auditoria.RegistrarAsync("TAREA_ARCHIVO", "DISABLE", $"Documento quitado de la tarea: {Recortar(adjunto.titulo)}", usuarioEmail, adjunto.id);

            return adjunto.fk_Tarea;
        }

        /// <summary>Abre un adjunto de tipo archivo (los enlaces no pasan por aquí).</summary>
        public async Task<(ArchivoDescarga contenido, string nombre)?> Abrir(int adjuntoId, CancellationToken ct)
        {
            var adjunto = await _context.TareaArchivo.AsNoTracking().FirstOrDefaultAsync(a => a.id == adjuntoId && a.estado, ct);
            if (adjunto == null || TutorService.EsEnlace(adjunto.ubicacion))
                return null;

            // Nombre de descarga: el título con la extensión real del archivo.
            var extension = Path.GetExtension(adjunto.ubicacion);
            var nombre = adjunto.titulo.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                ? adjunto.titulo
                : adjunto.titulo + extension;

            try
            {
                return (await _blob.DescargarAsync(TutorService.ContenedorMaterial(adjunto.tipo), adjunto.ubicacion, ct), nombre);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("El adjunto {AdjuntoId} no existe en Blob Storage.", adjuntoId);
                return null;
            }
        }

        private static TareaAdjuntoResponse Map(TareaArchivoModel a) => new()
        {
            id = a.id,
            titulo = a.titulo,
            tipo = a.tipo,
            esEnlace = TutorService.EsEnlace(a.ubicacion),
            url = TutorService.EsEnlace(a.ubicacion) ? a.ubicacion : null
        };

        private async Task BorrarBlob(string tipo, string blob)
        {
            try
            {
                await _blob.EliminarAsync(TutorService.ContenedorMaterial(tipo), blob);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo borrar el blob {Blob} de un adjunto de tarea.", blob);
            }
        }

        private static string Recortar(string texto) =>
            texto.Length > 120 ? texto[..120] + "…" : texto;
    }
}
