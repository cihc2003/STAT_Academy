using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Contrasena
{
    public class CambiarContrasenaViewModel
    {
        public int usuarioId { get; set; }

        [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string contrasenaActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña nueva es obligatoria.")]
        [MinLength(
            8,
            ErrorMessage = "La contraseña debe tener al menos 8 caracteres."
        )]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña nueva")]
        public string nuevaContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar la contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare(
            nameof(nuevaContrasena),
            ErrorMessage = "Las contraseñas no coinciden."
        )]
        public string confirmarContrasena { get; set; } = string.Empty;
    }
}