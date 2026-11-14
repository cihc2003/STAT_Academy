namespace STAT_Academy.DTOs.Curso
{
    public class CursoResponse
    {
        public int id { get; set; }

        public string nombre { get; set; }

        public string descripcion { get; set; }

        public decimal precio { get; set; }

        public int duracionSemanas { get; set; }

        public bool estado { get; set; }

        public DateTime fechaInicio { get; set; }

        public DateTime fechaFin { get; set; }
    }
}