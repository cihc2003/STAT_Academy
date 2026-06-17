using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models;

public class Order : AuditableEntity
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public decimal Total { get; set; }

    [Required(ErrorMessage = "El estado es obligatorio.")]
    [StringLength(30, ErrorMessage = "El estado no puede superar los 30 caracteres.")]
    public string Status { get; set; } = "Confirmado";
    public List<OrderItem> Items { get; set; } = [];
}
