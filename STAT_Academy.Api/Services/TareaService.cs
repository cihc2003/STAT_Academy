using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
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
            var inicioCurso = _context.Curso
                .Where(c => c.id == cursoId)
                .Select(c => (DateTime?)c.fechaInicio)
                .FirstOrDefault();

            var tareas = _context.Tarea
                .Where(x => x.CursoId == cursoId && x.Estado != false)
                .OrderBy(x => x.Fecha_Inicio)
                .ToList();

            return tareas
                .Select(x => new TareaResponse
                {
                    Id = x.ID,
                    Titulo = x.Titulo ?? string.Empty,
                    Descripcion = x.Descripcion ?? string.Empty,
                    FechaInicio = x.Fecha_Inicio ?? default,
                    FechaLimite = x.Fecha_Limite ?? default,
                    Entregada = x.Entregada ?? false,
                    // TAREA no tiene columna de semana: se calcula con la fecha de inicio.
                    Semana = inicioCurso == null
                        ? null
                        : CursoModel.SemanaDe(inicioCurso.Value, x.Fecha_Inicio)
                })
                .ToList();
        }
    }
}
