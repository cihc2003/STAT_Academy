using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Curso
{
    /// <summary>Alta o edición de un curso (solo Admin). Los largos coinciden con las columnas de CURSO.</summary>
    public class CursoCreateRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un tutor.")]
        public int fk_tutor { get; set; }

        /// <summary>Se ignora: el creador es siempre el Admin que hace la petición (sale del token).</summary>
        public int fk_creador { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre admite como máximo 100 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(255, ErrorMessage = "La descripción admite como máximo 255 caracteres.")]
        public string descripcion { get; set; } = string.Empty;

        [Range(0, 99999999.99, ErrorMessage = "El precio no es válido.")]
        public decimal precio { get; set; }

        public DateTime fechaInicio { get; set; }

        public DateTime fechaFin { get; set; }
    }
}
