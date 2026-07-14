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

        public ProveedorModel Crear(ProveedorCreateRequest request)
        {
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
            var proveedor = _context.Proveedor.FirstOrDefault(p => p.id == id);

            if (proveedor == null)
                return null;

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
                "admin"
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
    }
}