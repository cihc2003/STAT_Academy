namespace STAT_Academy.DTOs.Tareas
{
    public class TareaResponse
    {
        public int Id { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public DateTime FechaInicio { get; set; }

        public DateTime FechaLimite { get; set; }

        public bool Entregada { get; set; }

        public int? Semana { get; set; }
    }
}