using STAT_Academy.Api.Data;
using STAT_Academy.DTOs.Proveedor;
using STAT_Academy.Api.Models;

namespace STAT_Academy.Api.Services
{
    public class ProveedorService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;

        public ProveedorService(ApplicationDbContext context, AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
        }

        public List<ProveedorModel> GetAll()
        {
            return _context.Proveedor.ToList();
        }

        public ProveedorModel? GetById(int id)
        {
            return _context.Proveedor
                .FirstOrDefault(p => p.id == id);
        }
        public List<ProveedorModel> GetActivos()
        {
            return _context.Proveedor
                .Where(p => p.estado)
                .ToList();
        }
        public ProveedorModel Crear(ProveedorCreateRequest request)
        {
            bool existe = _context.Proveedor.Any(p => p.nombre == request.nombre);

            if (existe)
                throw new Exception("Ya existe un proveedor con ese nombre");

            var proveedor = new ProveedorModel
            {
                nombre = request.nombre,
                contacto = request.contacto,
                telefono = request.telefono,
                email = request.email,

                estado = true,
                fecha_creacion = DateTime.UtcNow,
                fecha_edicion = DateTime.UtcNow
            };

            _context.Proveedor.Add(proveedor);
            _context.SaveChanges();

            _auditoria.Registrar(
                "PROVEEDOR",
                "CREATE",
                $"Proveedor {proveedor.nombre} creado",
                "admin"
            );

            return proveedor;
        }

        public ProveedorModel? Editar(int id, ProveedorCreateRequest request)
        {
            bool existe = _context.Proveedor.Any(p =>
                p.nombre == request.nombre &&
                p.id != id);
            if (existe)
                throw new Exception("Ya existe un proveedor con ese nombre");

            var proveedor = _context.Proveedor.FirstOrDefault(p => p.id == id);

            if (proveedor == null)
                return null;
            if (!proveedor.estado)
                throw new Exception("El proveedor está desactivado");

            proveedor.nombre = request.nombre;
            proveedor.contacto = request.contacto;
            proveedor.telefono = request.telefono;
            proveedor.email = request.email;
            proveedor.fecha_edicion = DateTime.UtcNow;

            _context.SaveChanges();

            _auditoria.Registrar(
                "PROVEEDOR",
                "UPDATE",
                $"Proveedor {proveedor.nombre} editado",
                "admin",
                proveedor.id
            );
            return proveedor;
        }
        public ProveedorModel Desactivar(int id)
        {
            var proveedor = _context.Proveedor.FirstOrDefault(p => p.id == id);

            if (proveedor == null)
                return null;

            bool tieneProductosActivos = _context.Producto
                .Any(p => p.fk_proveedor == id && p.estado == true);

            if (tieneProductosActivos)
                throw new Exception("No se puede desactivar el proveedor porque tiene productos activos");

            proveedor.estado = false;
            proveedor.fecha_edicion = DateTime.UtcNow;

            _context.SaveChanges();

            _auditoria.Registrar(
                "PROVEEDOR",
                "DISABLE",
                $"Proveedor {proveedor.nombre} desactivado",
                "admin"
            );

            return proveedor;
        }
        public ProveedorModel Activar(int id)
        {
            var proveedor = _context.Proveedor
                .FirstOrDefault(p => p.id == id);

            if (proveedor == null)
                return null;

            proveedor.estado = true;
            proveedor.fecha_edicion = DateTime.UtcNow;

            _context.SaveChanges();

            _auditoria.Registrar(
                "PROVEEDOR",
                "ENABLE",
                $"Proveedor {proveedor.nombre} activado",
                "admin"
            );

            return proveedor;
        }
    }
}