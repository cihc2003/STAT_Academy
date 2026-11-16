using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;

namespace STAT_Academy.Api.Services
{
    public class AuditoriaService
    {
        private readonly ApplicationDbContext _context;

        public AuditoriaService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Guarda el registro de inmediato (y con él cualquier cambio pendiente del contexto).
        /// </summary>
        public async Task RegistrarAsync(string entidad, string accion, string descripcion, string usuario, int? entidadId = null)
        {
            var log = new AuditoriaModel
            {
                entidad = entidad,
                accion = accion,
                descripcion = descripcion,
                usuario = usuario,
                entidad_id = entidadId,
                fecha = DateTime.UtcNow
            };

            _context.Auditoria.Add(log);
            await _context.SaveChangesAsync();
        }

        public Task<List<AuditoriaModel>> GetAllAsync() =>
            _context.Auditoria.AsNoTracking().ToListAsync();

        public Task<List<AuditoriaModel>> FiltrarPorAccionAsync(string accion) =>
            _context.Auditoria.AsNoTracking()
                .Where(a => a.accion == accion)
                .ToListAsync();

        public Task<List<AuditoriaModel>> FiltrarPorEntidadAsync(string entidad) =>
            _context.Auditoria.AsNoTracking()
                .Where(a => a.entidad == entidad)
                .ToListAsync();

        public Task<List<AuditoriaModel>> FiltrarPorProductoAsync(int id) =>
            _context.Auditoria.AsNoTracking()
                .Where(a => a.entidad == "PRODUCTO" && a.entidad_id == id)
                .ToListAsync();
    }
}
