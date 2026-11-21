using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Contrasena
{
    public class CambiarContrasenaRequest
    {
        // El usuario se toma del token JWT; no viaja en el cuerpo.

        [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
        public string contrasenaActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña nueva es obligatoria.")]
        [MinLength(
            8,
            ErrorMessage = "La contraseña debe tener al menos 8 caracteres."
        )]
        public string nuevaContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar la contraseña.")]
        [Compare(
            nameof(nuevaContrasena),
            ErrorMessage = "Las contraseñas no coinciden."
        )]
        public string confirmarContrasena { get; set; } = string.Empty;
    }
}
