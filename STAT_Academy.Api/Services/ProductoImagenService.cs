using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Data;
using STAT_Academy.DTOs;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Storage;

namespace STAT_Academy.Api.Services
{
    public interface IProductoImagenService
    {
        Task<List<ArchivoDto>> ListarAsync(int productoId, CancellationToken ct = default);
        Task<ArchivoDto?> SubirAsync(int productoId, IFormFile archivo, bool esPrincipal, CancellationToken ct = default);
        Task<bool> MarcarPrincipalAsync(int imagenId, CancellationToken ct = default);
        Task<bool> EliminarAsync(int imagenId, CancellationToken ct = default);
    }

    public class ProductoImagenService : IProductoImagenService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBlobStorageService _blob;

        public ProductoImagenService(ApplicationDbContext context, IBlobStorageService blob)
        {
            _context = context;
            _blob = blob;
        }

        public async Task<List<ArchivoDto>> ListarAsync(int productoId, CancellationToken ct = default)
        {
            var imagenes = await _context.ProductoImagen
                .AsNoTracking()
                .Where(i => i.fk_producto == productoId && i.estado)
                .OrderByDescending(i => i.es_principal == true)
                .ThenBy(i => i.id)
                .ToListAsync(ct);

            return imagenes.Select(ADto).ToList();
        }

        public async Task<ArchivoDto?> SubirAsync(int productoId, IFormFile archivo, bool esPrincipal, CancellationToken ct = default)
        {
            // 1. ¿Existe el producto y está activo?
            //    (quita "&& p.estado" si también quieres permitir imágenes en productos inactivos)
            var existe = await _context.Producto
                .AnyAsync(p => p.id == productoId && p.estado, ct);
            if (!existe) return null;

            // 2. Subir a Azure (valida extensión y tamaño)
            var subido = await _blob.SubirAsync(archivo, ContenedorBlob.Imagenes, $"productos/{productoId}", ct);

            // 3. Solo una imagen principal por producto
            if (esPrincipal)
                await DesmarcarPrincipalesAsync(productoId, ct);

            // 4. Guardar en PRODUCTO_IMAGEN
            var registro = new ProductoImagenModel
            {
                fk_producto = productoId,
                imagen_url = subido.BlobName,
                nombre_archivo = subido.NombreOriginal,
                es_principal = esPrincipal,
                estado = true
            };
            _context.ProductoImagen.Add(registro);

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch
            {
                // 5. Si falla la BD, borrar el blob para no dejar huérfanos
                await _blob.EliminarAsync(ContenedorBlob.Imagenes, subido.BlobName, CancellationToken.None);
                throw;
            }

            return ADto(registro);
        }

        public async Task<bool> MarcarPrincipalAsync(int imagenId, CancellationToken ct = default)
        {
            var imagen = await _context.ProductoImagen
                .FirstOrDefaultAsync(i => i.id == imagenId && i.estado, ct);
            if (imagen is null) return false;

            await DesmarcarPrincipalesAsync(imagen.fk_producto, ct);
            imagen.es_principal = true;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> EliminarAsync(int imagenId, CancellationToken ct = default)
        {
            var imagen = await _context.ProductoImagen
                .FirstOrDefaultAsync(i => i.id == imagenId && i.estado, ct);
            if (imagen is null) return false;

            // Borrado lógico
            imagen.estado = false;
            imagen.es_principal = false;
            await _context.SaveChangesAsync(ct);

            // Borrado físico: el contenedor es público y la URL seguiría funcionando
            await _blob.EliminarAsync(ContenedorBlob.Imagenes, imagen.imagen_url, ct);
            return true;
        }

        private async Task DesmarcarPrincipalesAsync(int productoId, CancellationToken ct)
        {
            var principales = await _context.ProductoImagen
                .Where(i => i.fk_producto == productoId && i.es_principal == true)
                .ToListAsync(ct);

            foreach (var p in principales)
                p.es_principal = false;
        }

        private ArchivoDto ADto(ProductoImagenModel i) => new(
            i.id,
            i.nombre_archivo,
            null,
            i.es_principal == true,
            _blob.ObtenerUrlPublica(i.imagen_url));
    }
}