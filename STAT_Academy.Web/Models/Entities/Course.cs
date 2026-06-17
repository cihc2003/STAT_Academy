using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models;

public class Course : AuditableEntity
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(140, ErrorMessage = "El título no puede superar los 140 caracteres.")]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    [StringLength(80, ErrorMessage = "La categoría no puede superar los 80 caracteres.")]
    [Display(Name = "Categoría")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(700, ErrorMessage = "La descripción no puede superar los 700 caracteres.")]
    [Display(Name = "Descripción")]
    public string Description { get; set; } = string.Empty;

    [Range(1, 80, ErrorMessage = "La duración debe estar entre 1 y 80 semanas.")]
    [Display(Name = "Duración en semanas")]
    public int DurationWeeks { get; set; }

    [Range(0, 999999, ErrorMessage = "El precio no puede ser negativo.")]
    [Display(Name = "Precio")]
    public decimal Price { get; set; }

    [Display(Name = "Tutor")]
    public string? TutorId { get; set; }
    public ApplicationUser? Tutor { get; set; }
}
