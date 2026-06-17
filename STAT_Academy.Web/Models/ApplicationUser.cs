using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models;

public class ApplicationUser : IdentityUser
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(120, ErrorMessage = "El nombre completo no puede superar los 120 caracteres.")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "El documento no puede superar los 30 caracteres.")]
    public string DocumentId { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
