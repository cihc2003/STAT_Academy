namespace STAT_Academy.DTOs.EstudianteCurso
{
    public class CursoCompradoResponse
    {
        public int MatriculaId { get; set; }

        public int CursoId { get; set; }

        public string NombreCurso { get; set; }

        public string Descripcion { get; set; }

        /// <summary>Porcentaje del curso transcurrido (0-100), según las fechas y la hora de Costa Rica.</summary>
        public int Progreso { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime FechaFin { get; set; }

        public string Estado { get; set; }

        public DateTime FechaMatricula { get; set; }
    }
}