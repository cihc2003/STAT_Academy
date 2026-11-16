namespace STAT_Academy.DTOs.Curso
{
    public class CursoResponse
    {
        public int id { get; set; }

        public int fk_tutor { get; set; }

        public int fk_creador { get; set; }

        public string nombre { get; set; } = string.Empty;

        public string descripcion { get; set; } = string.Empty;

        public decimal precio { get; set; }

        public int duracionSemanas { get; set; }

        public bool estado { get; set; }

        public DateTime fechaInicio { get; set; }

        public DateTime fechaFin { get; set; }

        /// <summary>Tipo de curso (texto libre); null si no se indicó.</summary>
        public string? tipo { get; set; }

        /// <summary>Prerrequisitos (texto libre); null si no hay.</summary>
        public string? prerrequisitos { get; set; }

        /// <summary>Nombre del tutor asignado (null si no se encuentra).</summary>
        public string? tutorNombre { get; set; }

        /// <summary>URL pública de la portada, o null si el curso no tiene.</summary>
        public string? imagenPortada { get; set; }

        /// <summary>
        /// True si todavía se puede comprar o matricular: activo y con fecha de inicio
        /// de hoy en adelante (hora de Costa Rica).
        /// </summary>
        public bool inscripcionAbierta { get; set; }
    }
}
