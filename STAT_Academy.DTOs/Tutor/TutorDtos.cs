using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.DTOs.Tutor
{
    // ---------------------------------------------------------------------
    // Cursos
    // ---------------------------------------------------------------------

    public class TutorCursoResponse
    {
        public int id { get; set; }
        public string nombre { get; set; } = "";
        public string descripcion { get; set; } = "";
        public bool estado { get; set; }
        public DateTime fechaInicio { get; set; }
        public DateTime fechaFin { get; set; }
        public int duracionSemanas { get; set; }
        public int estudiantes { get; set; }
        public int tareas { get; set; }
        /// <summary>Entregas recibidas que todavía no tienen nota.</summary>
        public int entregasPorCalificar { get; set; }
        public int material { get; set; }
    }

    // ---------------------------------------------------------------------
    // Estudiantes y calificaciones
    // ---------------------------------------------------------------------

    public class TutorEstudianteResponse
    {
        public int matriculaId { get; set; }
        public int estudianteId { get; set; }
        public string nombre { get; set; } = "";
        public string email { get; set; } = "";
        public DateTime? fechaMatricula { get; set; }
        public string estadoMatricula { get; set; } = "";
        public int progreso { get; set; }
        public int tareasEntregadas { get; set; }
        public int tareasTotales { get; set; }
        /// <summary>Promedio de las tareas calificadas (0-100); null si no tiene notas.</summary>
        public decimal? promedio { get; set; }
    }

    public class TutorCalificacionesResponse
    {
        public List<TutorTareaColumna> tareas { get; set; } = new();
        public List<TutorFilaCalificaciones> estudiantes { get; set; } = new();
    }

    public class TutorTareaColumna
    {
        public int id { get; set; }
        public string titulo { get; set; } = "";
        public DateTime? fechaLimite { get; set; }
    }

    public class TutorFilaCalificaciones
    {
        public int estudianteId { get; set; }
        public string nombre { get; set; } = "";
        public string email { get; set; } = "";
        /// <summary>Una celda por tarea, en el mismo orden que <see cref="TutorCalificacionesResponse.tareas"/>.</summary>
        public List<TutorCeldaCalificacion> notas { get; set; } = new();
        public decimal? promedio { get; set; }
    }

    public class TutorCeldaCalificacion
    {
        public int tareaId { get; set; }
        public int? entregaId { get; set; }
        /// <summary>"Sin entregar", "Entregada" o "Calificada".</summary>
        public string estado { get; set; } = "";
        public decimal? calificacion { get; set; }
    }

    // ---------------------------------------------------------------------
    // Tareas y entregas
    // ---------------------------------------------------------------------

    public class TutorTareaResponse
    {
        public int id { get; set; }
        public int cursoId { get; set; }
        public string titulo { get; set; } = "";
        public string descripcion { get; set; } = "";
        public DateTime? fechaInicio { get; set; }
        public DateTime? fechaLimite { get; set; }
        public int? semana { get; set; }
        public bool estado { get; set; }
        public int entregas { get; set; }
        public int calificadas { get; set; }
        public int estudiantes { get; set; }
    }

    public class TutorTareaRequest
    {
        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(100, ErrorMessage = "El título admite como máximo 100 caracteres.")]
        public string titulo { get; set; } = "";

        [StringLength(255, ErrorMessage = "La descripción admite como máximo 255 caracteres.")]
        public string descripcion { get; set; } = "";

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        public DateTime fechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha límite es obligatoria.")]
        public DateTime fechaLimite { get; set; }
    }

    public class TutorEntregasResponse
    {
        public TutorTareaResponse tarea { get; set; } = new();
        public string curso { get; set; } = "";
        /// <summary>Todos los estudiantes matriculados, hayan entregado o no.</summary>
        public List<TutorEntregaResponse> entregas { get; set; } = new();
    }

    public class TutorEntregaResponse
    {
        public int? entregaId { get; set; }
        public int estudianteId { get; set; }
        public string estudiante { get; set; } = "";
        public string email { get; set; } = "";
        public DateTime? fechaEntrega { get; set; }
        /// <summary>True si se entregó después de la fecha límite.</summary>
        public bool tarde { get; set; }
        public string estado { get; set; } = "";
        public decimal? calificacion { get; set; }
        public string? feedback { get; set; }
        public string? enlace { get; set; }
        public List<TutorArchivoResponse> archivos { get; set; } = new();
    }

    public class TutorArchivoResponse
    {
        public int id { get; set; }
        public string nombre { get; set; } = "";
        public string tipo { get; set; } = "";
    }

    public class TutorCalificarRequest
    {
        [Required(ErrorMessage = "La calificación es obligatoria.")]
        [Range(0, 100, ErrorMessage = "La calificación debe estar entre 0 y 100.")]
        public decimal calificacion { get; set; }

        [StringLength(255, ErrorMessage = "La retroalimentación admite como máximo 255 caracteres.")]
        public string? feedback { get; set; }
    }

    // ---------------------------------------------------------------------
    // Material
    // ---------------------------------------------------------------------

    public class TutorMaterialResponse
    {
        public int id { get; set; }
        public string titulo { get; set; } = "";
        /// <summary>Etiqueta corta: PDF, Word, Excel, PowerPoint, Imagen, Texto o Enlace.</summary>
        public string tipo { get; set; } = "";
        public bool esEnlace { get; set; }
        /// <summary>Solo para enlaces; los archivos se descargan por la API.</summary>
        public string? url { get; set; }
    }
}
