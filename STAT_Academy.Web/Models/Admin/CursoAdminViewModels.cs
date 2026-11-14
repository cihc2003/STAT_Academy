using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Admin
{
    public class CursoAdminResponse
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
    }

    public class CursoFormViewModel
    {
        public int? id { get; set; }

        [Required(ErrorMessage = "El tutor es obligatorio.")]
        public int fk_tutor { get; set; }

        public int fk_creador { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        public string descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal precio { get; set; }

        [Required(ErrorMessage = "La duración es obligatoria.")]
        [Range(1, 999, ErrorMessage = "La duración debe ser mayor a 0.")]
        public int duracionSemanas { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        public DateTime fechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
        public DateTime fechaFin { get; set; }

        public bool estado { get; set; } = true;
    }
}