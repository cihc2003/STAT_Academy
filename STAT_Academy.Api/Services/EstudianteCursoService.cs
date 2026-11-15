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
                          // Los cursos inactivos dejan de estar disponibles para el estudiante.
                          where ec.EstudianteId == estudianteId && c.estado
                          select new
                          {
                              ec.IdMatricula,
                              c.id,
                              c.nombre,
                              c.descripcion,
                              ec.Estado,
                              ec.Fecha_Matricula,
                              c.fechaInicio,
                              c.fechaFin
                          }).ToList();

            return cursos.Select(x => new CursoCompradoResponse
            {
                MatriculaId = x.IdMatricula,
                CursoId = x.id,
                NombreCurso = x.nombre,
                Descripcion = x.descripcion,
                Estado = x.Estado,
                // Tiempo transcurrido del curso hasta hoy (hora de Costa Rica) sobre su duración total.
                // La columna ESTUDIANTE_CURSO.Progreso no se usa: nunca se actualizaba.
                Progreso = CursoModel.ProgresoDe(x.fechaInicio, x.fechaFin),
                FechaMatricula = x.Fecha_Matricula,
                FechaInicio = x.fechaInicio,
                FechaFin = x.fechaFin
            }).ToList();
        }
        /// <summary>
        /// Inscripción directa, solo para cursos gratuitos. Los cursos de pago se compran
        /// (CompraService.ComprarCurso) y la matrícula se crea al aprobarse el pago.
        /// </summary>
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

            // Un curso que ya inició no acepta inscripciones nuevas.
            if (!curso.inscripcionAbierta)
                return false;

            // Un curso de pago no se puede inscribir sin pagar.
            if (curso.precio > 0)
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