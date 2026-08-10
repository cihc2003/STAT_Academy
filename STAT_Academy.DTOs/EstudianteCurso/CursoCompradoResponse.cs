namespace STAT_Academy.DTOs.EstudianteCurso
{
    public class CursoCompradoResponse
    {
        public int MatriculaId { get; set; }

        public int CursoId { get; set; }

        public string NombreCurso { get; set; }

        public string Descripcion { get; set; }

        public int Progreso { get; set; }

        public string Estado { get; set; }

        public DateTime FechaMatricula { get; set; }
    }
}