using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models;

public class Invoice : AuditableEntity
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    [Required(ErrorMessage = "El número de factura es obligatorio.")]
    [StringLength(40, ErrorMessage = "El número de factura no puede superar los 40 caracteres.")]
    public string Number { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}
