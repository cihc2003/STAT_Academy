using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models;

public class Supplier : AuditableEntity
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, ErrorMessage = "El nombre no puede superar los 120 caracteres.")]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [StringLength(160, ErrorMessage = "El correo electrónico no puede superar los 160 caracteres.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
    [Display(Name = "Teléfono")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(220, ErrorMessage = "La dirección no puede superar los 220 caracteres.")]
    [Display(Name = "Dirección")]
    public string Address { get; set; } = string.Empty;
}
