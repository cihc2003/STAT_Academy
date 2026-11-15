using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Api.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [StringLength(150, ErrorMessage = "El correo admite como máximo 150 caracteres.")]
        public string email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(256, ErrorMessage = "La contraseña admite como máximo 256 caracteres.")]
        public string password { get; set; } = string.Empty;
    }
}
