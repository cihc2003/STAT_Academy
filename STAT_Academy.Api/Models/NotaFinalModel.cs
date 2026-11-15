using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAT_Academy.Api.Models
{
    /// <summary>
    /// Ajuste de la nota final que hace el tutor (dbo.NOTA_FINAL), una fila por matrícula.
    /// Si no hay fila, la nota final es la calculada a partir de las tareas.
    /// Script: Database/Scripts/2026-11-15_nota_final.sql
    /// </summary>
    [Table("NOTA_FINAL")]
    public class NotaFinalModel
    {
        [Key]
        [Column("ID")]
        public int id { get; set; }

        [Column("FK_Matricula")]
        public int fk_Matricula { get; set; }

        /// <summary>Nota de 0 a 100 (decimal(5,2)).</summary>
        [Column("Nota")]
        public decimal nota { get; set; }

        [Column("Comentario")]
        public string? comentario { get; set; }

        [Column("FK_Tutor")]
        public int? fk_Tutor { get; set; }

        /// <summary>UTC.</summary>
        [Column("Fecha_Edicion")]
        public DateTime fecha_Edicion { get; set; } = DateTime.UtcNow;
    }
}
