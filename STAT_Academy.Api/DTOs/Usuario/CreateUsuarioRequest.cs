using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Api.DTOs.Usuarios
{
    public class CreateUsuarioRequest
    {
        [Required]
        [EmailAddress]
        public string email { get; set; } = string.Empty;

        [Required]
        public string nombre { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string password { get; set; } = string.Empty;

        [Required]
        public int fk_Tipo_Usuario { get; set; }
    }
}
