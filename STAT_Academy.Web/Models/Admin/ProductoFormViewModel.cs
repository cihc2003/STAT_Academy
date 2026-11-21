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

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Range(0.01, 99999999.99, ErrorMessage = "El precio debe ser mayor que 0.")]
        public decimal precio_base { get; set; }

        [Required(ErrorMessage = "Las existencias son obligatorias.")]
        [Range(0, int.MaxValue, ErrorMessage = "Las existencias no pueden ser negativas.")]
        public int stock { get; set; }

        [Required(ErrorMessage = "El stock mínimo es obligatorio.")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo.")]
        public int min_stock { get; set; }

        [Required(ErrorMessage = "Debes seleccionar un proveedor.")]
        public int fk_proveedor { get; set; }

        // Se llena en el controlador antes de mostrar la vista; la vista la usa para pintar el <select>.
        public List<ProveedorResponse> ProveedoresDisponibles { get; set; } = [];

        /// <summary>Imágenes actuales (solo al editar).</summary>
        public List<ProductoImagenResponse> Imagenes { get; set; } = [];

        /// <summary>Imágenes a subir al crear el producto.</summary>
        [Display(Name = "Imágenes")]
        public List<IFormFile> NuevasImagenes { get; set; } = [];

        public const string AcceptImagenes = ".jpg,.jpeg,.png,.gif,.webp";
        public const long MaxBytesPeticion = 61L * 1024 * 1024;
    }
}
