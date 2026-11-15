namespace STAT_Academy.DTOs.EstudianteCurso
{
    public class CursoCompradoResponse
    {
        public int MatriculaId { get; set; }

        public int CursoId { get; set; }

        public string NombreCurso { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        /// <summary>Porcentaje del curso transcurrido (0-100), según las fechas y la hora de Costa Rica.</summary>
        public int Progreso { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime FechaFin { get; set; }

        public string Estado { get; set; } = string.Empty;

        public DateTime FechaMatricula { get; set; }
    }
}