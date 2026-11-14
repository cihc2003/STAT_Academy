using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace STAT_Academy.Web.Models.Blog
{
    public class BlogResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [JsonPropertyName("contenido")]
        public string Contenido { get; set; } = string.Empty;

        [JsonPropertyName("fecha_creacion")]
        public DateTime FechaCreacion { get; set; }

        [JsonPropertyName("fecha_edicion")]
        public DateTime? FechaEdicion { get; set; }

        [JsonPropertyName("estado")]
        public bool Estado { get; set; }

        [JsonPropertyName("fk_Autor")]
        public int FkAutor { get; set; }

        [JsonPropertyName("autor")]
        public string? Autor { get; set; }
    }

    public class CreateBlogViewModel
    {
        [Required(ErrorMessage = "El titulo es obligatorio.")]
        [JsonPropertyName("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El contenido es obligatorio.")]
        [JsonPropertyName("contenido")]
        public string Contenido { get; set; } = string.Empty;

        [JsonPropertyName("fk_Autor")]
        public int FkAutor { get; set; }
    }

    public class UpdateBlogViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El titulo es obligatorio.")]
        [JsonPropertyName("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El contenido es obligatorio.")]
        [JsonPropertyName("contenido")]
        public string Contenido { get; set; } = string.Empty;

        [JsonPropertyName("estado")]
        public bool Estado { get; set; }
    }
}