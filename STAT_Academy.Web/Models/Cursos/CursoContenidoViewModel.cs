namespace STAT_Academy.Web.Models.Cursos
{
    /// <summary>Aula del curso para el estudiante: tareas y material en listas simples.</summary>
    public class CursoContenidoViewModel
    {
        public CursoResponse Curso { get; set; } = null!;
        /// <summary>Tareas con el estado de la entrega propia, progreso y nota final.</summary>
        public AulaCurso Aula { get; set; } = new();
        public List<MaterialResponse> Material { get; set; } = [];
    }
}
