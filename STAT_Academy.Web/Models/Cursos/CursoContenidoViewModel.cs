namespace STAT_Academy.Web.Models.Cursos
{
    /// <summary>Aula del curso para el estudiante: tareas y material en listas simples.</summary>
    public class CursoContenidoViewModel
    {
        public CursoResponse Curso { get; set; } = null!;
        public List<TareaResponse> Tareas { get; set; } = [];
        public List<MaterialResponse> Material { get; set; } = [];
    }
}
