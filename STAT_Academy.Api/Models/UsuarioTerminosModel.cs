using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAT_Academy.Api.Models
{
    /// <summary>
    /// Aceptación de los términos y condiciones y de la política de cookies (dbo.USUARIO_TERMINOS),
    /// una fila por usuario con la última versión aceptada.
    /// Script: Database/Scripts/2026-11-16_usuario_terminos.sql
    /// </summary>
    [Table("USUARIO_TERMINOS")]
    public class UsuarioTerminosModel
    {
        [Key]
        [Column("FK_Usuario")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int fk_Usuario { get; set; }

        [Required]
        [StringLength(20)]
        [Column("Version_Terminos")]
        public string version_Terminos { get; set; } = string.Empty;

        /// <summary>UTC.</summary>
        [Column("Fecha_Aceptacion")]
        public DateTime fecha_Aceptacion { get; set; } = DateTime.UtcNow;
    }
}
