using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAT_Academy.Api.Models
{
    /// <summary>
    /// Bloqueo temporal de una cuenta por intentos fallidos (dbo.USUARIO_BLOQUEO), una fila por usuario.
    /// Script: Database/Scripts/2026-11-16_usuario_bloqueo.sql
    /// </summary>
    [Table("USUARIO_BLOQUEO")]
    public class UsuarioBloqueoModel
    {
        [Key]
        [Column("FK_Usuario")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int fk_Usuario { get; set; }

        /// <summary>UTC.</summary>
        [Column("Bloqueado_Hasta")]
        public DateTime bloqueado_Hasta { get; set; }

        /// <summary>UTC.</summary>
        [Column("Fecha_Edicion")]
        public DateTime fecha_Edicion { get; set; } = DateTime.UtcNow;
    }
}
