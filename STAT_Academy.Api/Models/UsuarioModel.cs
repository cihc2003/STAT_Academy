using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Api.Models
{
    public class UsuarioModel
    {
        public int id { get; set; }

        [Required]
        [EmailAddress]
        public string? email { get; set; }

        [Required]
        public string? nombre { get; set; }

        [Required]
        public string? password { get; set; }

        [Required]
        public bool estado { get; set; }

        [Required]
        public int intentos_login { get; set; }

        [Required]
        public DateTime fecha_creacion { get; set; }
        public DateTime? fecha_edicion { get; set; }
        public DateTime? ultimo_Login { get; set; }
        public string? reset_token_hash { get; set; }

        public DateTime? reset_token_expiracion { get; set; }

        [Required]
        public bool reset_token_usado { get; set; }
        public string? nuevo_email_pendiente { get; set; }

        public string? email_change_token_hash { get; set; }

        public DateTime? email_change_token_expiracion { get; set; }

        [Required]
        public int fk_Tipo_Usuario { get; set; }

    }
}
