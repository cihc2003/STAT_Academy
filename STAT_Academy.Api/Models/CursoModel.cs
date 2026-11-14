using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAT_Academy.Api.Models
{
    [Table("CURSO")]
    public class CursoModel
    {
        [Key]
        [Column("ID")]
        public int id { get; set; }

        [Column("FK_Tutor")]
        public int fk_tutor { get; set; }

        [Column("FK_Creador")]
        public int fk_creador { get; set; }

        [Column("Nombre")]
        public string nombre { get; set; } = string.Empty;

        [Column("Descripcion")]
        public string descripcion { get; set; } = string.Empty;

        [Column("Precio_Base")]
        public decimal precio { get; set; }

        [Column("Estado")]
        public bool estado { get; set; }

        [Column("Fecha_Inicio")]
        public DateTime fechaInicio { get; set; }

        [Column("Fecha_Fin")]
        public DateTime fechaFin { get; set; }

        /// <summary>
        /// La tabla CURSO no tiene columna de duración: se calcula a partir de
        /// las fechas (semanas completas o parciales, mínimo 1).
        /// </summary>
        [NotMapped]
        public int duracionSemanas => CalcularSemanas(fechaInicio, fechaFin);

        public static int CalcularSemanas(DateTime inicio, DateTime fin)
        {
            var dias = (fin.Date - inicio.Date).TotalDays + 1;
            return Math.Max(1, (int)Math.Ceiling(dias / 7));
        }

        /// <summary>
        /// Semana del curso (1..n) en la que cae una fecha, o null si cae antes del inicio.
        /// </summary>
        public static int? SemanaDe(DateTime inicioCurso, DateTime? fecha)
        {
            if (fecha == null || fecha.Value.Date < inicioCurso.Date)
                return null;

            return (int)((fecha.Value.Date - inicioCurso.Date).TotalDays / 7) + 1;
        }
    }
}
