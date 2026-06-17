using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models;

public class CourseTask : AuditableEntity
{
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(120, ErrorMessage = "El título no puede superar los 120 caracteres.")]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Las instrucciones son obligatorias.")]
    [StringLength(500, ErrorMessage = "Las instrucciones no pueden superar los 500 caracteres.")]
    [Display(Name = "Instrucciones")]
    public string Instructions { get; set; } = string.Empty;

    [Display(Name = "Fecha límite")]
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(7);
}
