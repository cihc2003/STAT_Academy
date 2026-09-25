using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Correo
{
    public class SolicitarCambioCorreoRequest
    {
        [Required]
        public int usuarioId { get; set; }

        [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
        public string contrasenaActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo nuevo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        public string nuevoEmail { get; set; } = string.Empty;
    }
}