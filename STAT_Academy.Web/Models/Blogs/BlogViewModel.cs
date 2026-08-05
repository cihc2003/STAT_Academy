namespace STAT_Academy.Web.Models
{
    public class BlogViewModel
    {
        public int id { get; set; }

        public string titulo { get; set; } = "";

        public string contenido { get; set; } = "";

        public DateTime fecha_creacion { get; set; }

        public DateTime? fecha_edicion { get; set; }

        public bool estado { get; set; }

        public int fk_Autor { get; set; }

        public string? Autor { get; set; }
    }
}