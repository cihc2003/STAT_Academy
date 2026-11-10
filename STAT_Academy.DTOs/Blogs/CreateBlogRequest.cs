namespace STAT_Academy.DTOs.Blogs
{
    public class CreateBlogRequest
    {
        public string titulo { get; set; } = "";

        public string contenido { get; set; } = "";

        public int fk_Autor { get; set; }
    }
}