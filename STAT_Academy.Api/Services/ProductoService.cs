using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Storage;
using STAT_Academy.DTOs.Producto;

namespace STAT_Academy.Api.Services
{
    /// <summary>
    /// Productos de la tienda e imágenes (PRODUCTO_IMAGEN en el contenedor público "imagenes").
    /// La lógica de imágenes sigue la de la rama Pruebas_Blob_Storage.
    /// </summary>
    public class ProductoService
    {
        public const int MaxImagenesPorProducto = 12;

        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly IBlobStorageService _blob;
        private readonly StockAlertaService _alertas;
        private readonly ILogger<ProductoService> _logger;

        public ProductoService(
            ApplicationDbContext context,
            AuditoriaService auditoria,
            IBlobStorageService blob,
            StockAlertaService alertas,
            ILogger<ProductoService> logger)
        {
            _context = context;
            _auditoria = auditoria;
            _blob = blob;
            _alertas = alertas;
            _logger = logger;
        }

        // ==================================================================
        // Consultas
        // ==================================================================

        /// <param name="incluirInactivos">Solo Admin: los clientes ven únicamente productos activos.</param>
        public List<ProductoResponse> GetAll(bool incluirInactivos)
        {
            return ConImagenes()
                .Where(p => incluirInactivos || p.estado)
                .OrderBy(p => p.nombre)
                .AsEnumerable()
                .Select(Map)
                .ToList();
        }

        public ProductoResponse? GetById(int id, bool incluirInactivos)
        {
            var producto = ConImagenes().FirstOrDefault(p => p.id == id && (incluirInactivos || p.estado));
            return producto == null ? null : Map(producto);
        }

        /// <summary>Productos activos con existencias en o por debajo del mínimo.</summary>
        public List<ProductoResponse> StockBajo()
        {
            return ConImagenes()
                .Where(p => p.estado && p.stock <= p.min_stock)
                .OrderBy(p => p.stock - p.min_stock)
                .AsEnumerable()
                .Select(Map)
                .ToList();
        }

        // ==================================================================
        // Altas y cambios
        // ==================================================================

        public (ProductoResponse? producto, string? error) Crear(ProductoCreateRequest request, string usuarioEmail)
        {
            if (ValidarProveedor(request.fk_proveedor) is string error)
                return (null, error);

            var producto = new ProductoModel
            {
                nombre = request.nombre.Trim(),
                categoria = request.categoria.Trim(),
                descripcion = request.descripcion.Trim(),
                precio_base = request.precio_base,
                stock = request.stock,
                min_stock = request.min_stock,
                fk_proveedor = request.fk_proveedor,
                estado = true,
            };

            _context.Producto.Add(producto);
            _context.SaveChanges();

            _auditoria.Registrar("PRODUCTO", "CREATE", $"Producto {producto.nombre} creado", usuarioEmail, producto.id);

            return (GetById(producto.id, incluirInactivos: true), null);
        }

        public async Task<(ProductoResponse? producto, string? error)> Editar(int id, ProductoCreateRequest request, string usuarioEmail)
        {
            var producto = _context.Producto.FirstOrDefault(p => p.id == id);
            if (producto == null)
                return (null, null);

            if (ValidarProveedor(request.fk_proveedor) is string error)
                return (null, error);

            var estabaBajo = producto.StockBajo;

            producto.nombre = request.nombre.Trim();
            producto.categoria = request.categoria.Trim();
            producto.descripcion = request.descripcion.Trim();
            producto.precio_base = request.precio_base;
            producto.stock = request.stock;
            producto.min_stock = request.min_stock;
            producto.fk_proveedor = request.fk_proveedor;

            _context.SaveChanges();

            _auditoria.Registrar("PRODUCTO", "UPDATE", $"Producto {producto.nombre} actualizado", usuarioEmail, producto.id);

            // Si el cambio lo deja en o por debajo del mínimo, se avisa (solo al cruzar el umbral).
            if (!estabaBajo && producto.StockBajo && producto.estado)
                await _alertas.Notificar([producto]);

            return (GetById(id, incluirInactivos: true), null);
        }

        public ProductoResponse? CambiarEstado(int id, bool activo, string usuarioEmail)
        {
            var producto = _context.Producto.FirstOrDefault(p => p.id == id);
            if (producto == null)
                return null;

            producto.estado = activo;
            _context.SaveChanges();

            _auditoria.Registrar("PRODUCTO", activo ? "ENABLE" : "DISABLE",
                $"Producto {producto.nombre} {(activo ? "reactivado" : "desactivado")}", usuarioEmail, producto.id);

            return GetById(id, incluirInactivos: true);
        }

        // ==================================================================
        // Imágenes
        // ==================================================================

        public async Task<(ProductoResponse? producto, List<string> errores)> AgregarImagenes(
            int productoId, IReadOnlyList<IFormFile> archivos, string usuarioEmail, CancellationToken ct)
        {
            if (!_context.Producto.Any(p => p.id == productoId))
                return (null, ["El producto no existe."]);

            if (archivos.Count == 0)
                return (null, ["Seleccione al menos una imagen."]);

            var existentes = _context.ProductoImagen.Where(i => i.fk_producto == productoId && i.estado).ToList();
            if (existentes.Count + archivos.Count > MaxImagenesPorProducto)
                return (null, [$"Un producto admite como máximo {MaxImagenesPorProducto} imágenes (ya tiene {existentes.Count})."]);

            // Validar todo antes de subir nada: solo imágenes, por extensión, tamaño y firma real.
            var errores = new List<string>();
            foreach (var archivo in archivos)
            {
                var (valido, error) = await BlogArchivoValidador.Validar(archivo);
                if (error != null)
                    errores.Add(error);
                else if (!valido!.EsImagen)
                    errores.Add($"'{valido.NombreOriginal}' no es una imagen (JPG, PNG, GIF o WEBP).");
            }

            if (errores.Count > 0)
                return (null, errores);

            var hayPrincipal = existentes.Any(i => i.es_principal);
            var subidos = new List<string>();

            try
            {
                foreach (var archivo in archivos)
                {
                    var subido = await _blob.SubirAsync(archivo, ContenedorBlob.Imagenes, $"productos/{productoId}", ct);
                    subidos.Add(subido.BlobName);

                    _context.ProductoImagen.Add(new ProductoImagenModel
                    {
                        fk_producto = productoId,
                        imagen_url = subido.BlobName,
                        nombre_archivo = subido.NombreOriginal,
                        // La primera imagen de un producto sin portada pasa a ser la principal.
                        es_principal = !hayPrincipal && subidos.Count == 1,
                        estado = true
                    });
                }

                _context.SaveChanges();
            }
            catch (ArchivoInvalidoException ex)
            {
                await BorrarBlobs(subidos);
                return (null, [ex.Message]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir imágenes del producto {ProductoId}.", productoId);
                await BorrarBlobs(subidos);
                return (null, ["No se pudieron guardar las imágenes. Intente nuevamente."]);
            }

            SincronizarImagenPrincipal(productoId);
            _auditoria.Registrar("PRODUCTO", "UPLOAD", $"{archivos.Count} imagen(es) agregada(s)", usuarioEmail, productoId);

            return (GetById(productoId, incluirInactivos: true), []);
        }

        public bool MarcarPrincipal(int productoId, int imagenId)
        {
            var imagenes = _context.ProductoImagen.Where(i => i.fk_producto == productoId && i.estado).ToList();
            if (imagenes.All(i => i.id != imagenId))
                return false;

            foreach (var i in imagenes)
                i.es_principal = i.id == imagenId;

            _context.SaveChanges();
            SincronizarImagenPrincipal(productoId);
            return true;
        }

        public async Task<bool> QuitarImagen(int productoId, int imagenId, string usuarioEmail)
        {
            var imagen = _context.ProductoImagen.FirstOrDefault(i => i.id == imagenId && i.fk_producto == productoId && i.estado);
            if (imagen == null)
                return false;

            var eraPrincipal = imagen.es_principal;
            imagen.estado = false;
            imagen.es_principal = false;

            // Si se quitó la portada, la siguiente imagen pasa a ser la principal.
            if (eraPrincipal)
            {
                var siguiente = _context.ProductoImagen
                    .Where(i => i.fk_producto == productoId && i.estado && i.id != imagenId)
                    .OrderBy(i => i.id)
                    .FirstOrDefault();

                if (siguiente != null)
                    siguiente.es_principal = true;
            }

            _context.SaveChanges();
            SincronizarImagenPrincipal(productoId);

            // El contenedor es público: si no se borra el archivo, la URL seguiría funcionando.
            await BorrarBlobs([imagen.imagen_url]);

            _auditoria.Registrar("PRODUCTO", "REMOVE_IMAGE", $"Imagen quitada: {imagen.nombre_archivo}", usuarioEmail, productoId);
            return true;
        }

        // ==================================================================
        // Ayudantes
        // ==================================================================

        private IQueryable<ProductoModel> ConImagenes() =>
            _context.Producto.AsNoTracking().Include(p => p.Imagenes.Where(i => i.estado));

        private string? ValidarProveedor(int fkProveedor) =>
            _context.Proveedor.Any(p => p.id == fkProveedor) ? null : "El proveedor seleccionado no existe.";

        /// <summary>Mantiene PRODUCTO.Imagen_URL igual al blob de la imagen principal.</summary>
        private void SincronizarImagenPrincipal(int productoId)
        {
            var principal = _context.ProductoImagen
                .Where(i => i.fk_producto == productoId && i.estado && i.es_principal)
                .Select(i => i.imagen_url)
                .FirstOrDefault();

            var producto = _context.Producto.First(p => p.id == productoId);
            if (producto.imagen_url != principal)
            {
                producto.imagen_url = principal;
                _context.SaveChanges();
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

        private ProductoResponse Map(ProductoModel p)
        {
            var imagenes = p.Imagenes
                .OrderByDescending(i => i.es_principal)
                .ThenBy(i => i.id)
                .Select(i => new ProductoImagenResponse
                {
                    id = i.id,
                    url = _blob.ObtenerUrlPublica(i.imagen_url),
                    nombre = i.nombre_archivo ?? "imagen",
                    es_principal = i.es_principal
                })
                .ToList();

            return new ProductoResponse
            {
                id = p.id,
                nombre = p.nombre,
                categoria = p.categoria,
                descripcion = p.descripcion,
                precio_base = p.precio_base,
                stock = p.stock,
                min_stock = p.min_stock,
                estado = p.estado,
                fk_proveedor = p.fk_proveedor,
                stock_bajo = p.StockBajo,
                agotado = p.stock <= 0,
                imagen_principal = imagenes.FirstOrDefault(i => i.es_principal)?.url ?? imagenes.FirstOrDefault()?.url,
                imagenes = imagenes
            };
        }
    }
}
