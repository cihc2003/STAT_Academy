using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Usuarios
{
    /// <summary>
    /// Edición de un usuario por un Admin. El correo NO se puede cambiar desde aquí
    /// (identifica la cuenta y tiene su propio flujo de confirmación).
    /// </summary>
    public class UpdateUsuarioRequest
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 150 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [Range(1, 3, ErrorMessage = "Seleccione un tipo de usuario válido.")]
        public int fk_Tipo_Usuario { get; set; }

        /// <summary>Opcional: si no se envía, el estado no cambia.</summary>
        public bool? estado { get; set; }
    }
}
