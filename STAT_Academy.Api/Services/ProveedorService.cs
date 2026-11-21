using Microsoft.EntityFrameworkCore;
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

        public Task<List<ProveedorModel>> GetAllAsync() =>
            _context.Proveedor.AsNoTracking().ToListAsync();

        public async Task<ProveedorModel> CrearAsync(ProveedorCreateRequest request)
        {
            var proveedor = new ProveedorModel
            {
                nombre = request.nombre,
                contacto = request.contacto,
                telefono = request.telefono,
                email = request.email,

                estado = true,
            };

            _context.Proveedor.Add(proveedor);
            await _context.SaveChangesAsync();

            await _auditoria.RegistrarAsync(
                "PROVEEDOR",
                "CREATE",
                $"Proveedor {proveedor.nombre} creado",
                "admin"
            );

            return proveedor;
        }

        public async Task<ProveedorModel?> EditarAsync(int id, ProveedorCreateRequest request)
        {
            var proveedor = await _context.Proveedor.FirstOrDefaultAsync(p => p.id == id);

            if (proveedor == null)
                return null;

            proveedor.nombre = request.nombre;
            proveedor.contacto = request.contacto;
            proveedor.telefono = request.telefono;
            proveedor.email = request.email;

            await _context.SaveChangesAsync();

            await _auditoria.RegistrarAsync(
                "PROVEEDOR",
                "UPDATE",
                $"Proveedor {proveedor.nombre} editado",
                "admin"
            );

            return proveedor;
        }
        public async Task<ProveedorModel?> DesactivarAsync(int id)
        {
            var proveedor = await _context.Proveedor.FirstOrDefaultAsync(p => p.id == id);

            if (proveedor == null)
                return null;

            bool tieneProductosActivos = await _context.Producto
                .AnyAsync(p => p.fk_proveedor == id && p.estado == true);

            if (tieneProductosActivos)
                throw new InvalidOperationException("No se puede desactivar el proveedor porque tiene productos activos");

            proveedor.estado = false;

            await _context.SaveChangesAsync();

            await _auditoria.RegistrarAsync(
                "PROVEEDOR",
                "DISABLE",
                $"Proveedor {proveedor.nombre} desactivado",
                "admin"
            );

            return proveedor;
        }

        public Task<ProveedorModel?> GetByIdAsync(int id) =>
            _context.Proveedor.AsNoTracking().FirstOrDefaultAsync(p => p.id == id);
    }
}