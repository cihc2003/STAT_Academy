using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.DTOs.EstudianteCurso;

namespace STAT_Academy.Api.Services
{
    public class EstudianteCursoService
    {
        private readonly ApplicationDbContext _context;

        public EstudianteCursoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<CursoCompradoResponse> ObtenerCursos(int estudianteId)
        {
            var cursos = (from ec in _context.EstudianteCurso
                          join c in _context.Set<CursoModel>()
                          on ec.CursoId equals c.id
                          where ec.EstudianteId == estudianteId
                          select new CursoCompradoResponse
                          {
                              MatriculaId = ec.IdMatricula,
                              CursoId = c.id,
                              NombreCurso = c.nombre,
                              Descripcion = c.descripcion,
                              Estado = ec.Estado,
                              Progreso = ec.Progreso,
                              FechaMatricula = ec.Fecha_Matricula
                          }).ToList();

            return cursos;
        }
    }
}