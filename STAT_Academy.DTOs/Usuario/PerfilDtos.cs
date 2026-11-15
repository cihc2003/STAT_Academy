using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Usuarios
{
    /// <summary>Datos de la cuenta del usuario autenticado (sin ids ni datos de seguridad).</summary>
    public class PerfilResponse
    {
        public string nombre { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        /// <summary>"Admin", "Tutor", "Cliente" o "Usuario".</summary>
        public string tipo { get; set; } = string.Empty;
        /// <summary>Fecha de registro según AUDITORIA (UTC); null si no hay registro.</summary>
        public DateTime? fecha_Registro { get; set; }
        /// <summary>Último inicio de sesión correcto (UTC).</summary>
        public DateTime? ultimo_Login { get; set; }
    }

    /// <summary>Cambio del nombre propio. El correo no se edita.</summary>
    public class ActualizarPerfilRequest
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 150 caracteres.")]
        public string nombre { get; set; } = string.Empty;
    }
}
