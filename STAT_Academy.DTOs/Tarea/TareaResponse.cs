namespace STAT_Academy.DTOs.Tareas
{
    public class TareaResponse
    {
        public int Id { get; set; }

        public string Titulo { get; set; }

        public string Descripcion { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime FechaLimite { get; set; }

        public bool Entregada { get; set; }

        public int? Semana { get; set; }
    }
}