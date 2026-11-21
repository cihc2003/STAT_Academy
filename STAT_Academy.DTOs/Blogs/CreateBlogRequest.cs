using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Blogs
{
    public class CreateBlogRequest
    {
        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(150, ErrorMessage = "El título admite como máximo 150 caracteres.")]
        public string titulo { get; set; } = "";

        [Required(ErrorMessage = "El contenido es obligatorio.")]
        public string contenido { get; set; } = "";

        // El autor NO viaja en la petición: la API usa el usuario del token.
    }
}
