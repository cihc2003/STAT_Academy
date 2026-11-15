using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Proveedor
{
    /// <summary>Alta o edición de un proveedor. Los largos coinciden con las columnas de PROVEEDOR.</summary>
    public class ProveedorCreateRequest
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre admite como máximo 100 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "El contacto admite como máximo 100 caracteres.")]
        public string? contacto { get; set; }

        [StringLength(50, ErrorMessage = "El teléfono admite como máximo 50 caracteres.")]
        public string? telefono { get; set; }

        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        [StringLength(100, ErrorMessage = "El correo admite como máximo 100 caracteres.")]
        public string? email { get; set; }
    }
}
