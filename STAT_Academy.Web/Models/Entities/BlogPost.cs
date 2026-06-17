using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models;

public class BlogPost : AuditableEntity
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(160, ErrorMessage = "El título no puede superar los 160 caracteres.")]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    [StringLength(120, ErrorMessage = "La categoría no puede superar los 120 caracteres.")]
    [Display(Name = "Categoría")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "El resumen es obligatorio.")]
    [StringLength(280, ErrorMessage = "El resumen no puede superar los 280 caracteres.")]
    [Display(Name = "Resumen")]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "El contenido es obligatorio.")]
    [Display(Name = "Contenido")]
    public string Content { get; set; } = string.Empty;
}
