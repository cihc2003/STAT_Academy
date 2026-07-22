using STAT_Academy.Api.Data;
using STAT_Academy.DTOs.Producto;
using STAT_Academy.Api.Models;

namespace STAT_Academy.Api.Services
{
    public class ProductoService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;

        public ProductoService(ApplicationDbContext context, AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
        }

        public List<ProductoModel> GetAll()
        {
            return _context.Producto.ToList();
        }
        public ProductoModel? GetById(int id)
        {
            return _context.Producto.FirstOrDefault(p => p.id == id);
        }
        public List<ProductoModel> GetActivos()
        {
            return _context.Producto
                .Where(p => p.estado)
                .ToList();
        }
        public ProductoModel Crear(ProductoCreateRequest request)
        {
            bool existe = _context.Producto.Any(p => p.nombre == request.nombre);

            if (existe)
                throw new Exception("Ya existe un producto con ese nombre");

            var proveedor = _context.Proveedor
                .FirstOrDefault(p => p.id == request.fk_proveedor);

            if (proveedor == null)
                throw new Exception("Proveedor no existe");

            var producto = new ProductoModel
            {
                nombre = request.nombre,
                categoria = request.categoria,
                descripcion = request.descripcion,
                precio_base = request.precio_base,
                stock = request.stock,
                min_stock = request.min_stock,
                fk_proveedor = request.fk_proveedor,
                estado = true,
                fecha_creacion = DateTime.UtcNow,
                fecha_edicion = DateTime.UtcNow
            };

            _context.Producto.Add(producto);
            _context.SaveChanges();

            _auditoria.Registrar(
                "PRODUCTO",
                "CREATE",
                $"Producto {producto.nombre} creado",
                "admin",
                producto.id
            );

            return producto;
        }

        public ProductoModel Editar(int id, ProductoCreateRequest request)
        {
            bool existe = _context.Producto.Any(p =>
                p.nombre == request.nombre &&
                p.id != id);

            if (existe)
                throw new Exception("Ya existe un producto con ese nombre");
            var producto = _context.Producto.FirstOrDefault(p => p.id == id);

            if (producto == null)
                return null;

            var proveedor = _context.Proveedor
                .FirstOrDefault(p => p.id == request.fk_proveedor);

            if (proveedor == null)
                throw new Exception("Proveedor no existe");
            if (!proveedor.estado)
                throw new Exception("El proveedor está desactivado");

            producto.nombre = request.nombre;
            producto.categoria = request.categoria;
            producto.descripcion = request.descripcion;
            producto.precio_base = request.precio_base;
            producto.stock = request.stock;
            producto.min_stock = request.min_stock;
            producto.fk_proveedor = request.fk_proveedor;
            producto.fecha_edicion = DateTime.UtcNow;

            _context.SaveChanges();

            _auditoria.Registrar(
                "PRODUCTO",
                "UPDATE",
                $"Producto {producto.nombre} actualizado",
                "admin",
                producto.id
            );

            return producto;
        }

        public ProductoModel Desactivar(int id)
        {
            var producto = _context.Producto.FirstOrDefault(p => p.id == id);

            if (producto == null)
                return null;
            if (!producto.estado)
                throw new Exception("El producto está desactivado");

            producto.estado = false;
            producto.fecha_edicion = DateTime.UtcNow;

            _context.SaveChanges();

            _auditoria.Registrar(
                "PRODUCTO",
                "DISABLE",
                $"Producto {producto.nombre} desactivado",
                "admin",
                producto.id
            );

            return producto;
        }
        public ProductoModel Activar(int id)
        {
            var producto = _context.Producto.FirstOrDefault(p => p.id == id);

            if (producto == null)
                return null;

            producto.estado = true;
            producto.fecha_edicion = DateTime.UtcNow;

            _context.SaveChanges();

            _auditoria.Registrar(
                "PRODUCTO",
                "ENABLE",
                $"Producto {producto.nombre} activado",
                "admin",
                producto.id
            );

            return producto;
        }

    }
}