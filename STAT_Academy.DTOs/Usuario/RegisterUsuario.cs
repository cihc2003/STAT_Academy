using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Usuarios
{
    public class RegisterUsuarioRequest
    {
        [Required]
        [EmailAddress]
        public string email { get; set; } = string.Empty;

        [Required]
        public string nombre { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string password { get; set; } = string.Empty;
    }
}
