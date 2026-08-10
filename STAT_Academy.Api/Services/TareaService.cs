using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Data;
using STAT_Academy.DTOs.Tareas;

namespace STAT_Academy.Api.Services
{
    public class TareaService
    {
        private readonly ApplicationDbContext _context;

        public TareaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<TareaResponse> ObtenerPorCurso(int cursoId)
        {
            return _context.Tarea
                .Where(x => x.CursoId == cursoId)
                .Select(x => new TareaResponse
                {
                    Id = x.ID,
                    Titulo = x.Titulo,
                    Descripcion = x.Descripcion,
                    FechaInicio = x.Fecha_Inicio,
                    FechaLimite = x.Fecha_Limite,
                    Entregada = x.Entregada
                })
                .ToList();
        }
    }
}