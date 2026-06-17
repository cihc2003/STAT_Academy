using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models;

public class Product : AuditableEntity
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(140, ErrorMessage = "El nombre no puede superar los 140 caracteres.")]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    [StringLength(80, ErrorMessage = "La categoría no puede superar los 80 caracteres.")]
    [Display(Name = "Categoría")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    [Display(Name = "Descripción")]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 999999, ErrorMessage = "El precio debe ser mayor que cero.")]
    [Display(Name = "Precio")]
    public decimal Price { get; set; }

    [Range(0, 99999, ErrorMessage = "Las existencias no pueden ser negativas.")]
    [Display(Name = "Existencias")]
    public int Stock { get; set; }

    [Display(Name = "Proveedor")]
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
}
