using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Producto
{
    public class ProductoCreateRequest
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre admite como máximo 100 caracteres.")]
        public string nombre { get; set; } = "";

        [Required(ErrorMessage = "La categoría es obligatoria.")]
        [StringLength(100, ErrorMessage = "La categoría admite como máximo 100 caracteres.")]
        public string categoria { get; set; } = "";

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(255, ErrorMessage = "La descripción admite como máximo 255 caracteres.")]
        public string descripcion { get; set; } = "";

        /// <summary>Mayor que 0 y dentro de decimal(10,2).</summary>
        [Range(typeof(decimal), "0.01", "99999999.99", ErrorMessage = "El precio debe ser mayor que 0 (máximo 99 999 999,99).")]
        public decimal precio_base { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Las existencias no pueden ser negativas.")]
        public int stock { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo.")]
        public int min_stock { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un proveedor.")]
        public int fk_proveedor { get; set; }
    }
}
