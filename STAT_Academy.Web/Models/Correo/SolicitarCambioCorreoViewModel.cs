using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Correo
{
    public class SolicitarCambioCorreoViewModel
    {
        public int usuarioId { get; set; }

        [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string contrasenaActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo nuevo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        [Display(Name = "Correo electrónico nuevo")]
        public string nuevoEmail { get; set; } = string.Empty;
    }
}