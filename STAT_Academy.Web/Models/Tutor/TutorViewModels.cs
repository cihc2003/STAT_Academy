using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Tutor
{
    // Respuestas de api/Tutor/* (los nombres coinciden con el JSON de la API).

    public class TutorCurso
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Estado { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int DuracionSemanas { get; set; }
        public int Estudiantes { get; set; }
        public int Tareas { get; set; }
        public int EntregasPorCalificar { get; set; }
        public int Material { get; set; }
    }

    public class TutorEstudiante
    {
        public int MatriculaId { get; set; }
        public int EstudianteId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? FechaMatricula { get; set; }
        public string EstadoMatricula { get; set; } = string.Empty;
        public int Progreso { get; set; }
        public int TareasEntregadas { get; set; }
        public int TareasTotales { get; set; }
        public decimal? Promedio { get; set; }
    }

    public class TutorCalificaciones
    {
        public List<TutorTareaColumna> Tareas { get; set; } = [];
        public List<TutorFilaCalificaciones> Estudiantes { get; set; } = [];
    }

    public class TutorTareaColumna
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public DateTime? FechaLimite { get; set; }
    }

    public class TutorFilaCalificaciones
    {
        public int EstudianteId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<TutorCeldaCalificacion> Notas { get; set; } = [];
        public decimal? Promedio { get; set; }
    }

    public class TutorCeldaCalificacion
    {
        public int TareaId { get; set; }
        public int? EntregaId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal? Calificacion { get; set; }
    }

    public class TutorTarea
    {
        public int Id { get; set; }
        public int CursoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaLimite { get; set; }
        public int? Semana { get; set; }
        public bool Estado { get; set; }
        public int Entregas { get; set; }
        public int Calificadas { get; set; }
        public int Estudiantes { get; set; }

        public bool Vencida => FechaLimite.HasValue && FechaLimite.Value < DateTime.Now;
    }

    public class TutorEntregas
    {
        public TutorTarea Tarea { get; set; } = new();
        public string Curso { get; set; } = string.Empty;
        public List<TutorEntrega> Entregas { get; set; } = [];
    }

    public class TutorEntrega
    {
        public int? EntregaId { get; set; }
        public int EstudianteId { get; set; }
        public string Estudiante { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? FechaEntrega { get; set; }
        public bool Tarde { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal? Calificacion { get; set; }
        public string? Feedback { get; set; }
        public string? Enlace { get; set; }
        public List<TutorArchivo> Archivos { get; set; } = [];
    }

    public class TutorArchivo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
    }

    public class TutorMaterial
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public bool EsEnlace { get; set; }
        public string? Url { get; set; }
    }

    // ---------------------------------------------------------------------
    // Pantallas
    // ---------------------------------------------------------------------

    /// <summary>Página principal de un curso: resumen, tareas y material.</summary>
    public class TutorCursoPagina
    {
        public TutorCurso Curso { get; set; } = new();
        public List<TutorTarea> Tareas { get; set; } = [];
        public List<TutorMaterial> Material { get; set; } = [];
        public MaterialFormViewModel NuevoMaterial { get; set; } = new();
    }

    public class TutorEstudiantesPagina
    {
        public TutorCurso Curso { get; set; } = new();
        public List<TutorEstudiante> Estudiantes { get; set; } = [];
    }

    public class TutorCalificacionesPagina
    {
        public TutorCurso Curso { get; set; } = new();
        public TutorCalificaciones Calificaciones { get; set; } = new();
    }

    public class TareaFormViewModel
    {
        public int? Id { get; set; }
        public int CursoId { get; set; }
        public string CursoNombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(100, ErrorMessage = "El título admite como máximo 100 caracteres.")]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "La descripción admite como máximo 255 caracteres.")]
        [Display(Name = "Instrucciones")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        [Display(Name = "Disponible desde")]
        public DateTime FechaInicio { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "La fecha límite es obligatoria.")]
        [Display(Name = "Fecha límite de entrega")]
        public DateTime FechaLimite { get; set; } = DateTime.Today.AddDays(7).AddHours(23).AddMinutes(59);
    }

    public class CalificarViewModel
    {
        public int EntregaId { get; set; }
        public int TareaId { get; set; }

        /// <summary>
        /// Texto tal como llega del formulario. Se interpreta aparte para que "85.5" y "85,5"
        /// valgan lo mismo sin importar la configuración regional del servidor.
        /// </summary>
        [Required(ErrorMessage = "La calificación es obligatoria.")]
        public string? Calificacion { get; set; }

        public decimal? CalificacionNumero =>
            decimal.TryParse(Calificacion?.Trim().Replace(',', '.'),
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var valor)
                ? valor
                : null;

        [StringLength(255, ErrorMessage = "La retroalimentación admite como máximo 255 caracteres.")]
        public string? Feedback { get; set; }
    }

    public class MaterialFormViewModel
    {
        public int CursoId { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(100, ErrorMessage = "El título admite como máximo 100 caracteres.")]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        [Display(Name = "Archivo")]
        public IFormFile? Archivo { get; set; }

        [Url(ErrorMessage = "Escriba una dirección web completa (https://…).")]
        [Display(Name = "o enlace")]
        public string? Enlace { get; set; }

        public const string Accept = ".pdf,.doc,.docx,.xlsx,.pptx,.txt,.csv,.jpg,.jpeg,.png,.gif,.webp";
        public const string Ayuda =
            "Documentos PDF, Word, Excel, PowerPoint, TXT o CSV (máx. 10 MB) o imágenes (máx. 5 MB). " +
            "Solo los estudiantes matriculados pueden descargarlo.";
        public const long MaxBytesPeticion = 11L * 1024 * 1024;
    }
}
