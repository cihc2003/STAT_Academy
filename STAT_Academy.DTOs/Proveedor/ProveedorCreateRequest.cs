using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Proveedor
{
    public class ProveedorCreateRequest
    {
        [Required]
        public string nombre { get; set; }

        [Required]
        public string contacto { get; set; }

        [Required]
        public string telefono { get; set; }

        [Required]
        [EmailAddress]
        public string email { get; set; }
    }
}
