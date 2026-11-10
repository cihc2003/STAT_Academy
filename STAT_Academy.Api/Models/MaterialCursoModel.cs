using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAT_Academy.Api.Models
{
    [Table("MATERIAL_CURSO")]
    public class MaterialCursoModel
    {
        [Key]
        [Column("ID")]
        public int id { get; set; }

        [Column("FK_Curso")]
        public int fk_Curso { get; set; }

        [Column("Titulo")]
        public string titulo { get; set; }

        // Ruta/URL del archivo o enlace 
        [Column("Ubicacion_Material")]
        public string ubicacionMaterial { get; set; }

        [Column("Tipo")]
        public string tipo { get; set; }

        [Column("Estado")]
        public bool estado { get; set; }

        [Column("FK_Autor")]
        public int fk_Autor { get; set; }
    }
}