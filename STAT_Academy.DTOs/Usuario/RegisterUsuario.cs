using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Usuarios
{
    public class RegisterUsuarioRequest
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        [StringLength(150, ErrorMessage = "El correo admite como máximo 150 caracteres.")]
        public string email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 150 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(256, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 256 caracteres.")]
        public string password { get; set; } = string.Empty;
    }
}
