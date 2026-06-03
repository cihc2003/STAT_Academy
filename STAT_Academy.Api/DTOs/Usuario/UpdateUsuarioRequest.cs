using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Api.DTOs.Usuarios
{
    public class UpdateUsuarioRequest
    {
        [Required]
        [EmailAddress]
        public string email { get; set; } = string.Empty;

        [Required]
        public string nombre { get; set; } = string.Empty;

        [Required]
        public int fk_Tipo_Usuario { get; set; }

        [Required]
        public bool estado { get; set; }
    }
}