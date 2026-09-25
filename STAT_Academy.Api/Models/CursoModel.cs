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
        public string nombre { get; set; }

        [Column("Descripcion")]
        public string descripcion { get; set; }

        [Column("Precio_Base")]
        public decimal precio { get; set; }

        [Column("Duracion_Semanas")]
        public int duracionSemanas { get; set; }

        [Column("Fecha_Creacion")]
        public DateTime fechaCreacion { get; set; }

        [Column("Fecha_Edicion")]
        public DateTime fechaEdicion { get; set; }

        [Column("Estado")]
        public bool estado { get; set; }

        [Column("Fecha_Inicio")]
        public DateTime fechaInicio { get; set; }

        [Column("Fecha_Fin")]
        public DateTime fechaFin { get; set; }
    }
}