using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Carrito
{
    public class AgregarAlCarritoRequest
    {
        [Required]
        public int usuarioId { get; set; }

        [Required]
        public int productoId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1.")]
        public int cantidad { get; set; }
    }
}