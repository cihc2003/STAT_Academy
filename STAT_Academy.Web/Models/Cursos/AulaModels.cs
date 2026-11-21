using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Cursos
{
    // Respuestas de api/Aula/* (los nombres coinciden con el JSON de la API).
    // Las fechas de las tareas y entregas vienen en hora de Costa Rica (no en UTC).

    public class AulaCurso
    {
        public int CursoId { get; set; }
        public string Curso { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int Progreso { get; set; }
        public List<AulaTarea> Tareas { get; set; } = [];
        public NotaFinal NotaFinal { get; set; } = new();
    }

    public class AulaTarea
    {
        public int Id { get; set; }
        public int CursoId { get; set; }
        public string Curso { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaLimite { get; set; }
        public int? Semana { get; set; }
        public bool Vencida { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaEntrega { get; set; }
        public bool Tarde { get; set; }
        public decimal? Calificacion { get; set; }
        public string? Feedback { get; set; }
        public string? Enlace { get; set; }
        public List<AulaArchivo> Archivos { get; set; } = [];
        public List<TareaAdjunto> Adjuntos { get; set; } = [];
        public bool PuedeModificar { get; set; }
        public bool CuentaComoCero { get; set; }

        public bool Entregada => FechaEntrega != null;
        public bool Calificada => Calificacion != null;

        /// <summary>Vence en los próximos 3 días y todavía no se entregó.</summary>
        public bool VencePronto =>
            !Entregada && !Vencida && FechaLimite != null
            && FechaLimite.Value <= Helpers.Formato.AhoraCostaRica.AddDays(3);

        /// <summary>Texto corto del estado de la entrega propia.</summary>
        public string Etiqueta =>
            Calificada ? "Calificada"
            : Entregada ? (Tarde ? "Entregada tarde" : "Entregada")
            : Vencida ? "Vencida"
            : VencePronto ? "Vence pronto"
            : "Pendiente";

        public string ClasePill =>
            Calificada ? "tutor-pill--ok"
            : Entregada ? ""
            : Vencida ? "aula-pill--vencida"
            : VencePronto ? "tutor-pill--alerta"
            : "tutor-pill--gris";
    }

    /// <summary>Documento o enlace que el tutor adjuntó a una tarea (guía, plantilla…).</summary>
    public class TareaAdjunto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public bool EsEnlace { get; set; }
        public string? Url { get; set; }
    }

    public class AulaArchivo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
    }

    /// <summary>Nota final sobre 100: la ajustada por el tutor o, si no hay, la calculada.</summary>
    public class NotaFinal
    {
        public decimal? Calculada { get; set; }
        public decimal? Ajustada { get; set; }
        public decimal? Final { get; set; }
        public string? Comentario { get; set; }
        public DateTime? FechaAjuste { get; set; }
        public int TareasConsideradas { get; set; }
        public int TareasTotales { get; set; }
        public int PorCalificar { get; set; }

        public bool EsAjustada => Ajustada != null;
    }

    /// <summary>Formulario de entrega: archivos nuevos (se suman) y enlace.</summary>
    public class EntregaFormViewModel
    {
        public int TareaId { get; set; }

        public List<IFormFile> Archivos { get; set; } = [];

        [Url(ErrorMessage = "Escriba una dirección web completa (https://…).")]
        [StringLength(255, ErrorMessage = "El enlace admite como máximo 255 caracteres.")]
        public string? Enlace { get; set; }

        public bool QuitarEnlace { get; set; }

        public const int MaxArchivos = 5;
        public const string Accept = ".pdf,.doc,.docx,.xlsx,.pptx,.txt,.csv,.jpg,.jpeg,.png,.gif,.webp";
        public const string Ayuda =
            "PDF, Word, Excel, PowerPoint, TXT o CSV (máx. 10 MB c/u) o imágenes (máx. 5 MB c/u). Hasta 5 archivos por entrega.";

        /// <summary>5 archivos de hasta 10 MB + margen.</summary>
        public const long MaxBytesPeticion = 51L * 1024 * 1024;
    }

    public class AulaTareaPagina
    {
        public AulaTarea Tarea { get; set; } = new();
        public EntregaFormViewModel Entrega { get; set; } = new();
    }
}
