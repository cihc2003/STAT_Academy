using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models;

public class CourseMaterial : AuditableEntity
{
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(120, ErrorMessage = "El título no puede superar los 120 caracteres.")]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "El enlace es obligatorio.")]
    [StringLength(350, ErrorMessage = "El enlace no puede superar los 350 caracteres.")]
    [Display(Name = "Enlace")]
    public string Url { get; set; } = string.Empty;

    [Range(1, 80, ErrorMessage = "La semana debe estar entre 1 y 80.")]
    [Display(Name = "Semana")]
    public int Week { get; set; } = 1;
}
