using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using STAT_Academy.DTOs.Curso;

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

        /// <summary>Texto libre corto (p. ej. "Virtual" o "Taller"); null si no se indicó.</summary>
        [Column("Tipo")]
        [MaxLength(CursoCreateRequest.TipoMaxLargo)]
        public string? tipo { get; set; }

        /// <summary>Conocimientos o cursos previos recomendados; null si no hay.</summary>
        [Column("Prerrequisitos")]
        [MaxLength(CursoCreateRequest.PrerrequisitosMaxLargo)]
        public string? prerrequisitos { get; set; }

        /// <summary>
        /// La tabla CURSO no tiene columna de duración: se calcula a partir de
        /// las fechas (semanas completas o parciales, mínimo 1).
        /// </summary>
        [NotMapped]
        public int duracionSemanas => CalcularSemanas(fechaInicio, fechaFin);

        /// <summary>Fecha de hoy en Costa Rica (UTC-6, sin horario de verano).</summary>
        public static DateTime HoyCostaRica => AhoraCostaRica.Date;

        /// <summary>
        /// Fecha y hora actual en Costa Rica. Las fechas de las tareas (inicio, límite y entrega)
        /// se guardan en hora local de Costa Rica, así que se comparan contra este valor.
        /// </summary>
        public static DateTime AhoraCostaRica => DateTime.UtcNow.AddHours(-6);

        /// <summary>
        /// Porcentaje del curso que ya transcurrió (0-100): días desde el inicio hasta hoy
        /// (ambos incluidos) sobre la duración total del curso, en hora de Costa Rica.
        /// Antes del inicio es 0; desde la fecha de fin, 100.
        /// </summary>
        public static int ProgresoDe(DateTime inicio, DateTime fin, DateTime? hoy = null)
        {
            var dia = (hoy ?? HoyCostaRica).Date;

            if (dia < inicio.Date)
                return 0;

            if (dia >= fin.Date)
                return 100;

            var total = (fin.Date - inicio.Date).TotalDays + 1;
            var transcurridos = (dia - inicio.Date).TotalDays + 1;

            return (int)Math.Clamp(Math.Floor(transcurridos / total * 100), 0, 100);
        }

        /// <summary>
        /// Se puede comprar o matricular mientras el curso esté activo y no haya iniciado
        /// (un curso que inicia hoy todavía acepta inscripciones).
        /// </summary>
        [NotMapped]
        public bool inscripcionAbierta => estado && fechaInicio.Date >= HoyCostaRica;

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
