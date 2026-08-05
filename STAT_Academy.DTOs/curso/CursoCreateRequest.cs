namespace STAT_Academy.DTOs.Curso
{
    public class CursoCreateRequest
    {
        public int fk_tutor { get; set; }

        public int fk_creador { get; set; }

        public string nombre { get; set; }

        public string descripcion { get; set; }

        public decimal precio { get; set; }

        public int duracionSemanas { get; set; }

        public DateTime fechaInicio { get; set; }

        public DateTime fechaFin { get; set; }
    }
}