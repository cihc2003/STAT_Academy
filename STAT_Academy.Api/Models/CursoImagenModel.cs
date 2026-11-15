using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAT_Academy.Api.Models
{
    /// <summary>
    /// Imagen de un curso (dbo.CURSO_IMAGEN), en el contenedor público "imagenes".
    /// La portada es la imagen activa con es_principal = true.
    /// Script: Database/Scripts/2026-11-14_curso_imagen.sql
    /// </summary>
    [Table("CURSO_IMAGEN")]
    public class CursoImagenModel
    {
        [Key]
        public int id { get; set; }

        public int fk_curso { get; set; }

        /// <summary>Nombre del blob: cursos/15/3f2a….webp</summary>
        public string imagen_url { get; set; } = "";

        public string? nombre_archivo { get; set; }

        public bool es_principal { get; set; }

        public bool estado { get; set; } = true;

        public DateTime fecha_creacion { get; set; } = DateTime.UtcNow;
    }
}
