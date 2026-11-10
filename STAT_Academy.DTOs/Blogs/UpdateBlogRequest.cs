namespace STAT_Academy.DTOs.Blogs
{
    public class UpdateBlogRequest
    {
        public string titulo { get; set; } = "";

        public string contenido { get; set; } = "";

        public bool estado { get; set; }
    }
}