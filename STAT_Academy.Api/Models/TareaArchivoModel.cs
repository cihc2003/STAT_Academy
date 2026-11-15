using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAT_Academy.Api.Models
{
    /// <summary>
    /// Documento o enlace que el tutor adjunta a una tarea (dbo.TAREA_ARCHIVO): guías, plantillas…
    /// Script: Database/Scripts/2026-11-15_tarea_archivo.sql
    /// </summary>
    [Table("TAREA_ARCHIVO")]
    public class TareaArchivoModel
    {
        [Key]
        [Column("ID")]
        public int id { get; set; }

        [Column("FK_Tarea")]
        public int fk_Tarea { get; set; }

        /// <summary>Nombre visible (el título o el nombre original del archivo).</summary>
        [Column("Titulo")]
        public string titulo { get; set; } = "";

        /// <summary>Nombre del blob, o la URL completa si es un enlace.</summary>
        [Column("Ubicacion")]
        public string ubicacion { get; set; } = "";

        /// <summary>Etiqueta corta: PDF, Word, Excel, PowerPoint, Texto, Imagen o Enlace.</summary>
        [Column("Tipo")]
        public string tipo { get; set; } = "";

        [Column("Estado")]
        public bool estado { get; set; } = true;

        [Column("FK_Autor")]
        public int? fk_Autor { get; set; }

        /// <summary>UTC.</summary>
        [Column("Fecha_Creacion")]
        public DateTime fecha_Creacion { get; set; } = DateTime.UtcNow;
    }
}
