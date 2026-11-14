using System.ComponentModel.DataAnnotations;
using STAT_Academy.Web.Models.Productos;

namespace STAT_Academy.Web.Models.Admin
{
    public class ProductoFormViewModel
    {
        // null cuando es un producto nuevo; con valor cuando se está editando uno existente.
        public int? id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoría es obligatoria.")]
        public string categoria { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        public string descripcion { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal precio_base { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
        public int stock { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo.")]
        public int min_stock { get; set; }

        [Required(ErrorMessage = "Debes seleccionar un proveedor.")]
        public int fk_proveedor { get; set; }

        // Se llena en el controlador antes de mostrar la vista; la vista la usa para pintar el <select>.
        public List<ProveedorResponse> ProveedoresDisponibles { get; set; } = [];
    }
}
