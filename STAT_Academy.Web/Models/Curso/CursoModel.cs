using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace STAT_Academy.Web.Models
{
    public class CursoModel
    {
        public int Id { get; set; }

        [JsonPropertyName("fk_tutor")]
        public int FK_Tutor { get; set; }

        [JsonPropertyName("fk_creador")]
        public int FK_Creador { get; set; }

        [Required]
        public string Nombre { get; set; }

        [Required]
        public string Descripcion { get; set; }

        [Required]
        [JsonPropertyName("precio")]
        public decimal Precio_Base { get; set; }

        [Required]
        [JsonPropertyName("duracionSemanas")]
        public int Duracion_Semanas { get; set; }

        public bool Estado { get; set; }

        [Required]
        [JsonPropertyName("fechaInicio")]
        public DateTime Fecha_Inicio { get; set; }

        [Required]
        [JsonPropertyName("fechaFin")]
        public DateTime Fecha_Fin { get; set; }
    }
}