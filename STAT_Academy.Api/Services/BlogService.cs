using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Storage;
using STAT_Academy.DTOs.Blogs;

namespace STAT_Academy.Api.Services
{
    /// <summary>
    /// Archivos del blog en Azure Blob Storage:
    ///  - Imágenes  → contenedor público "imagenes"  (se muestran con su URL directa).
    ///  - Documentos → contenedor privado "documentos" (se descargan a través de la API,
    ///    que respeta las mismas reglas que la entrada: inactiva = solo Admin).
    /// En ENTRADA_BLOG_ARCHIVO.Archivo_URL se guarda el NOMBRE del blob ("blog/12/abc.pdf"),
    /// no una URL, para no depender del dominio de la cuenta de almacenamiento.
    /// </summary>
    public class BlogService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly IBlobStorageService _blob;
        private readonly ILogger<BlogService> _logger;
        private readonly BlogContenidoService _contenido;

        public BlogService(
            ApplicationDbContext context,
            AuditoriaService auditoria,
            IBlobStorageService blob,
            ILogger<BlogService> logger,
            BlogContenidoService contenido)
        {
            _context = context;
            _auditoria = auditoria;
            _blob = blob;
            _logger = logger;
            _contenido = contenido;
        }

        // ------------------------------------------------------------------
        // Consultas
        // ------------------------------------------------------------------

        /// <param name="incluirInactivos">Solo para administradores: los clientes ven únicamente entradas activas.</param>
        public List<BlogResponse> GetBlogs(bool incluirInactivos)
        {
            return ConsultaConDetalle()
                .Where(b => incluirInactivos || b.estado)
                .OrderByDescending(b => b.fecha_Creacion)
                .AsEnumerable()
                .Select(Map)
                .ToList();
        }

        public BlogResponse? GetBlogById(int id, bool incluirInactivos)
        {
            var blog = ConsultaConDetalle()
                .FirstOrDefault(b => b.id == id && (incluirInactivos || b.estado));

            return blog == null ? null : Map(blog);
        }

        /// <summary>Autor de la entrada (activa o no), o null si no existe.</summary>
        public int? ObtenerAutorId(int id)
        {
            return _context.EntradaBlog
                .Where(b => b.id == id)
                .Select(b => (int?)b.fk_Autor)
                .FirstOrDefault();
        }

        // ------------------------------------------------------------------
        // Entradas
        // ------------------------------------------------------------------

        private const string ContenidoVacio = "El contenido no puede estar vacío.";

        public (BlogResponse? blog, string? error) CrearBlog(CreateBlogRequest request, int autorId, string autorEmail)
        {
            // El HTML del editor se limpia ANTES de guardarlo (lista blanca, sin scripts).
            var contenido = _contenido.Limpiar(request.contenido);
            if (_contenido.EstaVacio(contenido))
                return (null, ContenidoVacio);

            var blog = new EntradaBlogModel
            {
                titulo = request.titulo.Trim(),
                contenido = contenido,
                fk_Autor = autorId,
                estado = true,
                // La fecha la pone el servidor en el momento de crear, nunca el cliente.
                fecha_Creacion = DateTime.UtcNow
            };

            _context.EntradaBlog.Add(blog);
            _context.SaveChanges();

            _auditoria.Registrar("BLOG", "CREATE", $"Blog creado: {Recortar(blog.titulo)}", autorEmail, blog.id);

            return (GetBlogById(blog.id, incluirInactivos: true), null);
        }

        /// <returns>(null, null) si la entrada no existe; (null, error) si el contenido no es válido.</returns>
        public (BlogResponse? blog, string? error) ActualizarBlog(int id, UpdateBlogRequest request, string usuarioEmail)
        {
            var blog = _context.EntradaBlog.FirstOrDefault(x => x.id == id);

            if (blog == null)
                return (null, null);

            var contenido = _contenido.Limpiar(request.contenido);
            if (_contenido.EstaVacio(contenido))
                return (null, ContenidoVacio);

            blog.titulo = request.titulo.Trim();
            blog.contenido = contenido;
            blog.estado = request.estado;
            blog.fecha_Edicion = DateTime.UtcNow;

            _context.SaveChanges();

            _auditoria.Registrar("BLOG", "UPDATE", $"Blog actualizado: {Recortar(blog.titulo)}", usuarioEmail, blog.id);

            return (GetBlogById(id, incluirInactivos: true), null);
        }

        /// <summary>
        /// Sube una imagen para insertarla DENTRO del texto (el editor la coloca donde el autor quiera).
        /// Devuelve la URL pública. No se registra como adjunto: forma parte del contenido.
        /// </summary>
        public async Task<(string? url, string? error)> SubirImagenContenido(IFormFile? archivo, CancellationToken ct)
        {
            if (archivo == null || archivo.Length == 0)
                return (null, "Seleccione una imagen.");

            var (valido, error) = await BlogArchivoValidador.Validar(archivo);
            if (error != null)
                return (null, error);

            if (!valido!.EsImagen)
                return (null, "Solo se pueden insertar imágenes (JPG, PNG, GIF o WEBP).");

            try
            {
                var subido = await _blob.SubirAsync(archivo, ContenedorBlob.Imagenes, $"blog/contenido/{DateTime.UtcNow:yyyy/MM}", ct);
                return (_blob.ObtenerUrlPublica(subido.BlobName), null);
            }
            catch (ArchivoInvalidoException ex)
            {
                return (null, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir una imagen del contenido del blog.");
                return (null, "No se pudo subir la imagen. Intente nuevamente.");
            }
        }

        public BlogResponse? DesactivarBlog(int id, string usuarioEmail)
        {
            var blog = _context.EntradaBlog.FirstOrDefault(x => x.id == id);

            if (blog == null)
                return null;

            blog.estado = false;
            blog.fecha_Edicion = DateTime.UtcNow;

            _context.SaveChanges();

            _auditoria.Registrar("BLOG", "DISABLE", $"Blog desactivado: {Recortar(blog.titulo)}", usuarioEmail, blog.id);

            return GetBlogById(id, incluirInactivos: true);
        }

        // ------------------------------------------------------------------
        // Archivos adjuntos (imágenes y documentos)
        // ------------------------------------------------------------------

        public async Task<(BlogResponse? blog, List<string> errores)> AgregarArchivos(
            int blogId,
            IReadOnlyList<IFormFile> archivos,
            string usuarioEmail)
        {
            var errores = new List<string>();

            if (archivos.Count == 0)
                return (null, ["Debe seleccionar al menos un archivo."]);

            if (archivos.Count > BlogArchivoValidador.MaxArchivosPorSubida)
                return (null, [$"Puede subir como máximo {BlogArchivoValidador.MaxArchivosPorSubida} archivos a la vez."]);

            var existentes = _context.EntradaBlogArchivo
                .Where(a => a.fk_Entrada_Blog == blogId && a.estado)
                .ToList();

            if (existentes.Count + archivos.Count > BlogArchivoValidador.MaxArchivosPorEntrada)
                return (null, [$"Una entrada admite como máximo {BlogArchivoValidador.MaxArchivosPorEntrada} archivos (ya tiene {existentes.Count})."]);

            // 1) Validar todo antes de subir nada (extensión, tamaño y firma real del contenido).
            var validados = new List<(IFormFile archivo, BlogArchivoValidador.ArchivoValidado datos)>();
            foreach (var archivo in archivos)
            {
                var (valido, error) = await BlogArchivoValidador.Validar(archivo);
                if (error != null)
                    errores.Add(error);
                else
                    validados.Add((archivo, valido!));
            }

            if (errores.Count > 0)
                return (null, errores);

            // 2) Subir a Blob Storage y registrar en la base.
            var hayPrincipal = existentes.Any(a => a.es_Principal && a.EsImagen);
            var subidos = new List<(ContenedorBlob contenedor, string blobName)>();

            try
            {
                foreach (var (archivo, datos) in validados)
                {
                    var contenedor = datos.EsImagen ? ContenedorBlob.Imagenes : ContenedorBlob.Documentos;
                    var subido = await _blob.SubirAsync(archivo, contenedor, $"blog/{blogId}");
                    subidos.Add((contenedor, subido.BlobName));

                    var esPrincipal = datos.EsImagen && !hayPrincipal;
                    hayPrincipal |= esPrincipal;

                    _context.EntradaBlogArchivo.Add(new EntradaBlogArchivoModel
                    {
                        fk_Entrada_Blog = blogId,
                        nombre_Archivo = datos.NombreOriginal,
                        archivo_URL = subido.BlobName,
                        tipo_Archivo = subido.ContentType,
                        es_Principal = esPrincipal,
                        estado = true
                    });
                }

                _context.SaveChanges();
            }
            catch (ArchivoInvalidoException ex)
            {
                await EliminarDelAlmacenamiento(subidos);
                return (null, [ex.Message]);
            }
            catch (Exception ex)
            {
                // Si algo falla a mitad, no dejar archivos huérfanos en el almacenamiento.
                _logger.LogError(ex, "Error al subir adjuntos del blog {BlogId}.", blogId);
                await EliminarDelAlmacenamiento(subidos);
                return (null, ["No se pudieron guardar los archivos. Intente nuevamente."]);
            }

            _auditoria.Registrar("BLOG", "UPLOAD", $"{validados.Count} archivo(s) agregado(s)", usuarioEmail, blogId);

            return (GetBlogById(blogId, incluirInactivos: true), errores);
        }

        public async Task<bool> QuitarArchivo(int blogId, int archivoId, string usuarioEmail)
        {
            var archivo = _context.EntradaBlogArchivo
                .FirstOrDefault(a => a.id == archivoId && a.fk_Entrada_Blog == blogId && a.estado);

            if (archivo == null)
                return false;

            archivo.estado = false;
            var eraPrincipal = archivo.es_Principal;
            archivo.es_Principal = false;

            // Si se quitó la portada, la siguiente imagen pasa a ser la principal.
            if (eraPrincipal)
            {
                var siguiente = _context.EntradaBlogArchivo
                    .Where(a => a.fk_Entrada_Blog == blogId && a.estado && a.id != archivoId
                                && a.tipo_Archivo != null && a.tipo_Archivo.StartsWith("image/"))
                    .OrderBy(a => a.id)
                    .FirstOrDefault();

                if (siguiente != null)
                    siguiente.es_Principal = true;
            }

            _context.SaveChanges();

            await EliminarDelAlmacenamiento([(Contenedor(archivo), archivo.archivo_URL)]);

            _auditoria.Registrar("BLOG", "REMOVE_FILE", $"Archivo quitado: {Recortar(archivo.nombre_Archivo ?? "")}", usuarioEmail, blogId);

            return true;
        }

        public (bool exitoso, string mensaje) MarcarPrincipal(int blogId, int archivoId)
        {
            var archivos = _context.EntradaBlogArchivo
                .Where(a => a.fk_Entrada_Blog == blogId && a.estado)
                .ToList();

            var elegido = archivos.FirstOrDefault(a => a.id == archivoId);

            if (elegido == null)
                return (false, "El archivo no existe en esta entrada.");

            if (!elegido.EsImagen)
                return (false, "Solo una imagen puede ser la portada.");

            foreach (var a in archivos)
                a.es_Principal = a.id == archivoId;

            _context.SaveChanges();

            return (true, "Portada actualizada.");
        }

        // ------------------------------------------------------------------

        private IQueryable<EntradaBlogModel> ConsultaConDetalle() =>
            _context.EntradaBlog
                .AsNoTracking()
                .Include(b => b.Autor)
                .Include(b => b.Archivos.Where(a => a.estado));

        /// <summary>
        /// Abre un documento adjunto para descargarlo a través de la API.
        /// Devuelve null si la entrada o el archivo no existen, o si la entrada está
        /// inactiva y quien pide no es Admin (mismas reglas que ver la entrada).
        /// </summary>
        public async Task<(ArchivoDescarga contenido, string nombre)?> AbrirDocumento(
            int blogId, int archivoId, bool incluirInactivos, CancellationToken ct)
        {
            var archivo = _context.EntradaBlogArchivo
                .AsNoTracking()
                .Where(a => a.id == archivoId && a.fk_Entrada_Blog == blogId && a.estado)
                .Where(a => _context.EntradaBlog.Any(b => b.id == blogId && (incluirInactivos || b.estado)))
                .FirstOrDefault();

            if (archivo == null || archivo.EsImagen)
                return null;

            try
            {
                var descarga = await _blob.DescargarAsync(ContenedorBlob.Documentos, archivo.archivo_URL, ct);
                return (descarga, archivo.nombre_Archivo ?? "documento");
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("El documento {ArchivoId} del blog {BlogId} no existe en Blob Storage.", archivoId, blogId);
                return null;
            }
        }

        private static ContenedorBlob Contenedor(EntradaBlogArchivoModel archivo) =>
            archivo.EsImagen ? ContenedorBlob.Imagenes : ContenedorBlob.Documentos;

        private async Task EliminarDelAlmacenamiento(IEnumerable<(ContenedorBlob contenedor, string blobName)> blobs)
        {
            foreach (var (contenedor, blobName) in blobs)
            {
                try
                {
                    await _blob.EliminarAsync(contenedor, blobName);
                }
                catch (Exception ex)
                {
                    // El registro ya está inactivo; un archivo huérfano no rompe nada.
                    _logger.LogWarning(ex, "No se pudo borrar {Blob} de Blob Storage.", blobName);
                }
            }
        }

        private static string Recortar(string texto) =>
            texto.Length > 120 ? texto[..120] + "…" : texto;

        private static DateTime Utc(DateTime fecha) =>
            DateTime.SpecifyKind(fecha, DateTimeKind.Utc);

        private BlogResponse Map(EntradaBlogModel blog)
        {
            var archivos = blog.Archivos
                .OrderByDescending(a => a.es_Principal)
                .ThenBy(a => a.id)
                .Select(a => new BlogArchivoResponse
                {
                    id = a.id,
                    nombre = a.nombre_Archivo ?? "archivo",
                    // Imágenes: URL pública directa. Documentos: ruta de descarga de la API
                    // (el contenedor de documentos es privado).
                    url = a.EsImagen
                        ? _blob.ObtenerUrlPublica(a.archivo_URL)
                        : $"api/Blog/{blog.id}/archivos/{a.id}/descargar",
                    tipo = a.tipo_Archivo ?? "",
                    es_imagen = a.EsImagen,
                    es_principal = a.es_Principal
                })
                .ToList();

            return new BlogResponse
            {
                id = blog.id,
                titulo = blog.titulo,
                // Se vuelve a limpiar al leer: también cubre entradas antiguas en texto plano.
                contenido = _contenido.Limpiar(blog.contenido),
                resumen = BlogContenidoService.Resumen(blog.contenido),
                minutos_lectura = BlogContenidoService.MinutosLectura(blog.contenido),
                estado = blog.estado,
                fk_Autor = blog.fk_Autor,
                autor = blog.Autor?.nombre ?? blog.Autor?.email ?? "",
                fecha_creacion = Utc(blog.fecha_Creacion),
                fecha_edicion = blog.fecha_Edicion.HasValue ? Utc(blog.fecha_Edicion.Value) : null,
                imagen_principal = archivos.FirstOrDefault(a => a.es_imagen && a.es_principal)?.url
                                   ?? archivos.FirstOrDefault(a => a.es_imagen)?.url,
                archivos = archivos
            };
        }
    }
}
