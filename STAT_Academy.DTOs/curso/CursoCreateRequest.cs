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

        /// <summary>Largo de la columna CURSO.Tipo (NVARCHAR(50)).</summary>
        public const int TipoMaxLargo = 50;

        /// <summary>Largo de la columna CURSO.Prerrequisitos (NVARCHAR(500)).</summary>
        public const int PrerrequisitosMaxLargo = 500;

        /// <summary>Opcional, texto libre (p. ej. "Virtual" o "Taller").</summary>
        [StringLength(TipoMaxLargo, ErrorMessage = "El tipo admite como máximo 50 caracteres.")]
        public string? tipo { get; set; }

        /// <summary>Opcional, texto libre: conocimientos o cursos previos recomendados.</summary>
        [StringLength(PrerrequisitosMaxLargo, ErrorMessage = "Los prerrequisitos admiten como máximo 500 caracteres.")]
        public string? prerrequisitos { get; set; }

        /// <summary>Quita espacios de los extremos; un texto vacío o solo con espacios se guarda como null.</summary>
        public static string? Limpiar(string? texto) =>
            string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    }
}
