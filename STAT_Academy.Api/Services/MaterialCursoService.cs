using STAT_Academy.Api.Data;
using STAT_Academy.DTOs.MaterialCurso;

namespace STAT_Academy.Api.Services
{
    public class MaterialCursoService
    {
        private readonly ApplicationDbContext _context;

        public MaterialCursoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<MaterialCursoResponse> ObtenerPorCurso(int cursoId)
        {
            return _context.MaterialCurso
                .Where(m => m.fk_Curso == cursoId && m.estado)
                .OrderBy(m => m.semana)
                .Select(m => new MaterialCursoResponse
                {
                    Id = m.id,
                    Titulo = m.titulo,
                    UbicacionMaterial = m.ubicacionMaterial,
                    Tipo = m.tipo,
                    Semana = m.semana,
                    FechaCreacion = m.fechaCreacion
                })
                .ToList();
        }
    }
}