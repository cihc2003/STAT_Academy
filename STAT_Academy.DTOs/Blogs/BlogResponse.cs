namespace STAT_Academy.DTOs.Blogs
{
    public class BlogResponse
    {
        public int id { get; set; }

        public string titulo { get; set; } = "";

        public string contenido { get; set; } = "";

        public DateTime fecha_creacion { get; set; }

        public DateTime? fecha_edicion { get; set; }

        public bool estado { get; set; }

        public int fk_Autor { get; set; }
        public string autor { get; set; }
    }
}