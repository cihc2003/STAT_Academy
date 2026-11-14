using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAT_Academy.Api.Models
{
    /// <summary>Entrega de un estudiante para una tarea (dbo.ENTREGA_TAREA).</summary>
    [Table("ENTREGA_TAREA")]
    public class EntregaTareaModel
    {
        public const string EstadoEntregada = "Entregada";
        public const string EstadoCalificada = "Calificada";

        [Key]
        [Column("ID")]
        public int id { get; set; }

        [Column("FK_Tarea")]
        public int? fk_Tarea { get; set; }

        [Column("FK_Estudiante")]
        public int? fk_Estudiante { get; set; }

        /// <summary>Enlace externo de la entrega (opcional; los archivos van en ENTREGA_TAREA_ARCHIVO).</summary>
        [Column("Archivo_Entrega_URL")]
        public string? archivo_Entrega_URL { get; set; }

        [Column("Fecha_Entrega")]
        public DateTime? fecha_Entrega { get; set; }

        /// <summary>Nota de 0 a 100 (decimal(5,2)).</summary>
        [Column("Calificacion")]
        public decimal? calificacion { get; set; }

        [Column("Feedback")]
        public string? feedback { get; set; }

        [Column("Estado")]
        public string? estado { get; set; }

        public List<EntregaTareaArchivoModel> Archivos { get; set; } = new();
    }

    /// <summary>Archivo adjunto a una entrega (dbo.ENTREGA_TAREA_ARCHIVO).</summary>
    [Table("ENTREGA_TAREA_ARCHIVO")]
    public class EntregaTareaArchivoModel
    {
        [Key]
        [Column("ID")]
        public int id { get; set; }

        [Column("FK_Entrega_Tarea")]
        public int fk_Entrega_Tarea { get; set; }

        [Column("Nombre_Archivo")]
        public string? nombre_Archivo { get; set; }

        /// <summary>Nombre del blob en el contenedor privado "documentos".</summary>
        [Column("Archivo_URL")]
        public string archivo_URL { get; set; } = "";

        [Column("Tipo_Archivo")]
        public string? tipo_Archivo { get; set; }

        [Column("Estado")]
        public bool estado { get; set; } = true;
    }
}
