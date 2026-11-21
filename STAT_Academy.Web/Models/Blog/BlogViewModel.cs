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

        /// <summary>HTML ya limpiado por la API (lista blanca): se puede mostrar con Html.Raw.</summary>
        [JsonPropertyName("contenido")]
        public string Contenido { get; set; } = string.Empty;

        /// <summary>Texto plano corto para tarjetas.</summary>
        [JsonPropertyName("resumen")]
        public string Resumen { get; set; } = string.Empty;

        [JsonPropertyName("minutos_lectura")]
        public int MinutosLectura { get; set; } = 1;

        /// <summary>UTC (la API la asigna al crear la entrada).</summary>
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

        [JsonPropertyName("imagen_principal")]
        public string? ImagenPrincipal { get; set; }

        [JsonPropertyName("archivos")]
        public List<BlogArchivoResponse> Archivos { get; set; } = [];

        [JsonIgnore]
        public IEnumerable<BlogArchivoResponse> Imagenes => Archivos.Where(a => a.EsImagen);

        [JsonIgnore]
        public IEnumerable<BlogArchivoResponse> Documentos => Archivos.Where(a => !a.EsImagen);
    }

    public class BlogArchivoResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [JsonPropertyName("es_imagen")]
        public bool EsImagen { get; set; }

        [JsonPropertyName("es_principal")]
        public bool EsPrincipal { get; set; }

        /// <summary>Etiqueta corta para mostrar junto al documento (PDF, DOCX…).</summary>
        [JsonIgnore]
        public string Extension =>
            Path.GetExtension(Nombre).TrimStart('.').ToUpperInvariant() is { Length: > 0 } ext ? ext : "ARCHIVO";
    }

    public class CreateBlogViewModel
    {
        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(150, ErrorMessage = "El título admite como máximo 150 caracteres.")]
        [Display(Name = "Título")]
        [JsonPropertyName("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El contenido es obligatorio.")]
        [JsonPropertyName("contenido")]
        public string Contenido { get; set; } = string.Empty;

        /// <summary>Imágenes y documentos a adjuntar (se suben después de crear la entrada).</summary>
        [JsonIgnore]
        [Display(Name = "Imágenes y documentos")]
        public List<IFormFile> Archivos { get; set; } = [];
    }

    public class UpdateBlogViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(150, ErrorMessage = "El título admite como máximo 150 caracteres.")]
        [Display(Name = "Título")]
        [JsonPropertyName("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El contenido es obligatorio.")]
        [JsonPropertyName("contenido")]
        public string Contenido { get; set; } = string.Empty;

        [JsonPropertyName("estado")]
        [Display(Name = "Publicada (visible para los clientes)")]
        public bool Estado { get; set; }

        /// <summary>Adjuntos actuales, solo para mostrarlos en la página de edición.</summary>
        [JsonIgnore]
        public List<BlogArchivoResponse> ArchivosActuales { get; set; } = [];
    }

    /// <summary>Límites que se muestran en los formularios (deben coincidir con la API).</summary>
    public static class BlogArchivoLimites
    {
        public const string Accept = ".jpg,.jpeg,.png,.gif,.webp,.pdf,.doc,.docx,.xlsx,.pptx,.txt,.csv";
        public const string Ayuda =
            "Imágenes JPG, PNG, GIF o WEBP (máx. 5 MB) y documentos PDF, Word, Excel, PowerPoint, TXT o CSV (máx. 10 MB). " +
            "Hasta 10 archivos por subida y 20 por entrada. La primera imagen se usa como portada.";
        public const long MaxBytesPeticion = 110L * 1024 * 1024;
    }
}
