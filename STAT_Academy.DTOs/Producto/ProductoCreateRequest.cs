
using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Producto
{
    public class ProductoCreateRequest
    {
        [Required]
        public string nombre { get; set; }

        [Required]
        public string categoria { get; set; }

        [Required]
        public string descripcion { get; set; }

        [Range(0.01, 999999)]
        public decimal precio_base { get; set; }

        [Range(0, 999999)]
        public int stock { get; set; }

        [Range(0, 999999)]
        public int min_stock { get; set; }

        [Required]
        public int fk_proveedor { get; set; }
    }
}