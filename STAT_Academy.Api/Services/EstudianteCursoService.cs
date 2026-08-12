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
        public bool Matricular(int estudianteId, int cursoId)
        {
            // Verificar que el curso exista
            var curso = _context.Set<CursoModel>()
                .FirstOrDefault(c => c.id == cursoId);

            if (curso == null)
                return false;

            // Verificar que el curso esté activo
            if (!curso.estado)
                return false;

            // Verificar si ya está matriculado
            var yaMatriculado = _context.EstudianteCurso
                .Any(ec =>
                    ec.EstudianteId == estudianteId &&
                    ec.CursoId == cursoId);

            if (yaMatriculado)
                return false;

            // Crear matrícula
            var matricula = new EstudianteCursoModel
            {
                CursoId = cursoId,
                EstudianteId = estudianteId,
                Fecha_Matricula = DateTime.Now,
                Estado = "Activo",
                Progreso = 0
            };

            _context.EstudianteCurso.Add(matricula);

            _context.SaveChanges();

            return true;
        }
    }
}