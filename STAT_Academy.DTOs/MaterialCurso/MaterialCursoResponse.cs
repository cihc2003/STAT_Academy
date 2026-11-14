namespace STAT_Academy.DTOs.MaterialCurso
{
    public class MaterialCursoResponse
    {
        public int Id { get; set; }

        public string Titulo { get; set; }

        public string UbicacionMaterial { get; set; }

        public string Tipo { get; set; }

        public int? Semana { get; set; }

        public DateTime FechaCreacion { get; set; }
    }
}