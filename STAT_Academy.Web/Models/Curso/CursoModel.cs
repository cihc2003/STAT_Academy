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
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public string Descripcion { get; set; } = string.Empty;

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

        /// <summary>Tipo de curso (texto libre, p. ej. "Virtual"); null si no se indicó.</summary>
        [JsonPropertyName("tipo")]
        public string? Tipo { get; set; }

        /// <summary>Conocimientos o cursos previos recomendados; null si no hay.</summary>
        [JsonPropertyName("prerrequisitos")]
        public string? Prerrequisitos { get; set; }

        [JsonPropertyName("tutorNombre")]
        public string? TutorNombre { get; set; }

        /// <summary>URL pública de la portada; null si el curso no tiene.</summary>
        [JsonPropertyName("imagenPortada")]
        public string? ImagenPortada { get; set; }

        /// <summary>False si el curso ya inició o está inactivo (no se puede comprar ni inscribir).</summary>
        [JsonPropertyName("inscripcionAbierta")]
        public bool InscripcionAbierta { get; set; }
    }
}