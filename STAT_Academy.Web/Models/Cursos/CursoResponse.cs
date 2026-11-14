namespace STAT_Academy.Web.Models.Cursos
{
 
    public class CursoResponse
    {
        public int id { get; set; }
        public int fk_tutor { get; set; }
        public int fk_creador { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string descripcion { get; set; } = string.Empty;
        public decimal precio { get; set; }
        public int duracionSemanas { get; set; }
        public bool estado { get; set; }
        public DateTime fechaInicio { get; set; }
        public DateTime fechaFin { get; set; }
    }

    public class CursoCompradoResponse
    {
        public int MatriculaId { get; set; }
        public int CursoId { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int Progreso { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaMatricula { get; set; }
    }

    public class TareaResponse
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaLimite { get; set; }
        public bool Entregada { get; set; }
        public int? Semana { get; set; }
    }

    public class MaterialResponse
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string UbicacionMaterial { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int? Semana { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}