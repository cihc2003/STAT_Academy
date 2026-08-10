namespace STAT_Academy.Web.Models
{
    public class TareaModel
    {
        public int Id { get; set; }

        public int FK_Curso { get; set; }

        public string Titulo { get; set; }

        public string Descripcion { get; set; }

        public DateTime Fecha_Inicio { get; set; }

        public DateTime Fecha_Limite { get; set; }

        public bool Entregada { get; set; }

        public bool Estado { get; set; }
    }
}