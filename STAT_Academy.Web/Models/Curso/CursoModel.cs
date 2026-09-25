using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models
{
    public class CursoModel
    {
        public int Id { get; set; }

        public int FK_Tutor { get; set; }

        public int FK_Creador { get; set; }

        [Required]
        public string Nombre { get; set; }

        [Required]
        public string Descripcion { get; set; }

        [Required]
        public decimal Precio_Base { get; set; }

        [Required]
        public int Duracion_Semanas { get; set; }

        public bool Estado { get; set; }

        [Required]
        public DateTime Fecha_Inicio { get; set; }

        [Required]
        public DateTime Fecha_Fin { get; set; }
    }
}