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
                .Where(m => m.fk_Curso == cursoId && m.estado == true)
                .Select(m => new MaterialCursoResponse
                {
                    Id = m.id,
                    Titulo = m.titulo ?? string.Empty,
                    // El nombre del campo en el DTO se mantiene para no romper la Web;
                    // en la base la columna es Material_URL.
                    UbicacionMaterial = m.materialUrl ?? string.Empty,
                    Tipo = m.tipo ?? string.Empty,
                    // MATERIAL_CURSO no tiene semana ni fecha: aparece en "Sin semana".
                    Semana = null
                })
                .ToList();
        }
    }
}
