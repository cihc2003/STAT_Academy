using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;

namespace STAT_Academy.Api.Services
{
    public class AuditoriaService
    {
        private readonly ApplicationDbContext _context;

        public List<AuditoriaModel> FiltrarPorUsuario(string usuario)
        {
            return _context.Auditoria
                .Where(a => a.usuario == usuario)
                .ToList();
        }


        public AuditoriaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Registrar(string entidad, string accion, string descripcion, string usuario, int? entidadId = null)
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
            _context.SaveChanges();
        }

        public List<AuditoriaModel> GetAll()
        {
            return _context.Auditoria.ToList();
        }

        public List<AuditoriaModel> FiltrarPorAccion(string accion)
        {
            return _context.Auditoria
                .Where(a => a.accion == accion)
                .ToList();
        }

        public List<AuditoriaModel> FiltrarPorEntidad(string entidad)
        {
            return _context.Auditoria
                .Where(a => a.entidad == entidad)
                .ToList();
        }
        public List<AuditoriaModel> FiltrarPorProducto(int id)
        {
            return _context.Auditoria
                .Where(a => a.entidad == "PRODUCTO" && a.entidad_id == id)
                .ToList();
        }
        public List<AuditoriaModel> FiltrarPorProveedor(int id)
        {
            return _context.Auditoria
                .Where(a => a.entidad == "PROVEEDOR"
                         && a.entidad_id == id)
                .ToList();
        }
    }
}