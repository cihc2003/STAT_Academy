using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Productos
{
    public class ProductoCreateRequest
    {
        [Required]
        public string nombre { get; set; } = string.Empty;

        [Required]
        public string categoria { get; set; } = string.Empty;

        [Required]
        public string descripcion { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal precio_base { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int stock { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int min_stock { get; set; }

        [Required(ErrorMessage = "Debes seleccionar un proveedor.")]
        public int fk_proveedor { get; set; }
    }
}
