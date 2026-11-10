using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Blogs
{
    public class UpdateBlogRequest
    {
        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(150, ErrorMessage = "El título admite como máximo 150 caracteres.")]
        public string titulo { get; set; } = "";

        [Required(ErrorMessage = "El contenido es obligatorio.")]
        public string contenido { get; set; } = "";

        public bool estado { get; set; }
    }
}
