using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Contrasena
{
    public class SolicitarRecuperacionRequest
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        public string email { get; set; } = string.Empty;
    }
}