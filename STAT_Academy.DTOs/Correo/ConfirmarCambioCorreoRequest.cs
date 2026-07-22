using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Correo
{
    public class ConfirmarCambioCorreoRequest
    {
        [Required(ErrorMessage = "El token es obligatorio.")]
        public string token { get; set; } = string.Empty;
    }
}