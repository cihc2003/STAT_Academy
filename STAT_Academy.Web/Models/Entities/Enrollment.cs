using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models;

public class Enrollment : AuditableEntity
{
    [Required(ErrorMessage = "El estudiante es obligatorio.")]
    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser? Student { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }

    [Required(ErrorMessage = "El estado es obligatorio.")]
    [StringLength(30, ErrorMessage = "El estado no puede superar los 30 caracteres.")]
    public string Status { get; set; } = "Pendiente";
}
