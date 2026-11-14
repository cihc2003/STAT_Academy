namespace STAT_Academy.Web.Models.Cursos
{
    public class SemanaViewModel
    {
        public int Numero { get; set; }
        public List<TareaResponse> Tareas { get; set; } = [];
        public List<MaterialResponse> Material { get; set; } = [];

        public bool TieneContenido => Tareas.Count > 0 || Material.Count > 0;
    }

    public class CursoContenidoViewModel
    {
        public CursoResponse Curso { get; set; } = null!;
        public List<SemanaViewModel> Semanas { get; set; } = [];

        // Tareas que quedaron sin número de semana asignado.
        public SemanaViewModel SinSemana { get; set; } = new();
    }
}