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
        public string? tipo { get; set; }
        public string? prerrequisitos { get; set; }
        public string? imagenPortada { get; set; }
    }

    public class CursoFormViewModel
    {
        public int? id { get; set; }

        [Required(ErrorMessage = "El tutor es obligatorio.")]
        public int fk_tutor { get; set; }

        public int fk_creador { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre admite como máximo 100 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(255, ErrorMessage = "La descripción admite como máximo 255 caracteres.")]
        public string descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal precio { get; set; }

        // Solo lectura: la API la calcula a partir de las fechas.
        public int duracionSemanas { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        public DateTime fechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
        public DateTime fechaFin { get; set; }

        public bool estado { get; set; } = true;

        [Display(Name = "Tipo")]
        [StringLength(50, ErrorMessage = "El tipo admite como máximo 50 caracteres.")]
        public string? tipo { get; set; }

        [Display(Name = "Prerrequisitos")]
        [StringLength(500, ErrorMessage = "Los prerrequisitos admiten como máximo 500 caracteres.")]
        public string? prerrequisitos { get; set; }

        /// <summary>Sugerencias para el campo Tipo (es texto libre: se puede escribir otro).</summary>
        public static readonly string[] TiposSugeridos = ["Virtual", "Presencial", "Híbrido", "Taller", "Certificación"];

        /// <summary>Nueva portada (opcional): al guardar reemplaza la actual.</summary>
        [Display(Name = "Imagen de portada")]
        public IFormFile? Portada { get; set; }

        /// <summary>URL pública de la portada actual (solo lectura).</summary>
        public string? ImagenPortadaActual { get; set; }

        public const string AcceptImagenes = ProductoFormViewModel.AcceptImagenes;

        /// <summary>Una imagen de hasta 5 MB más el resto del formulario.</summary>
        public const long MaxBytesPeticion = 6L * 1024 * 1024;
    }
}