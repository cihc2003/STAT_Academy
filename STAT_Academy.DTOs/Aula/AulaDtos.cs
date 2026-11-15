namespace STAT_Academy.DTOs.Aula
{
    // ---------------------------------------------------------------------
    // Aula del estudiante (api/Aula/*): sus tareas, entregas y calificaciones.
    // ---------------------------------------------------------------------

    public class AulaCursoResponse
    {
        public int cursoId { get; set; }
        public string curso { get; set; } = "";
        public DateTime fechaInicio { get; set; }
        public DateTime fechaFin { get; set; }
        /// <summary>Porcentaje del curso transcurrido (0-100), según las fechas y la hora de Costa Rica.</summary>
        public int progreso { get; set; }
        public List<AulaTareaResponse> tareas { get; set; } = new();
        public NotaFinalResponse notaFinal { get; set; } = new();
    }

    public class AulaTareaResponse
    {
        public int id { get; set; }
        public int cursoId { get; set; }
        public string curso { get; set; } = "";
        public string titulo { get; set; } = "";
        public string descripcion { get; set; } = "";
        public DateTime? fechaInicio { get; set; }
        public DateTime? fechaLimite { get; set; }
        public int? semana { get; set; }
        /// <summary>True si la fecha límite ya pasó (hora de Costa Rica).</summary>
        public bool vencida { get; set; }

        /// <summary>"Sin entregar", "Entregada" o "Calificada".</summary>
        public string estado { get; set; } = "";
        public DateTime? fechaEntrega { get; set; }
        /// <summary>True si se entregó (o se modificó por última vez) después de la fecha límite.</summary>
        public bool tarde { get; set; }
        public decimal? calificacion { get; set; }
        public string? feedback { get; set; }
        public string? enlace { get; set; }
        public List<AulaArchivoResponse> archivos { get; set; } = new();

        /// <summary>Se puede entregar o cambiar la entrega mientras no esté calificada.</summary>
        public bool puedeModificar { get; set; }
        /// <summary>Vencida y sin entregar: cuenta como 0 en la nota final.</summary>
        public bool cuentaComoCero { get; set; }
    }

    public class AulaArchivoResponse
    {
        public int id { get; set; }
        public string nombre { get; set; } = "";
        public string tipo { get; set; } = "";
    }

    // ---------------------------------------------------------------------
    // Nota final (la usan el aula y el panel del tutor)
    // ---------------------------------------------------------------------

    public class NotaFinalResponse
    {
        /// <summary>
        /// Promedio automático (0-100): tareas calificadas + tareas vencidas sin entregar (cuentan 0).
        /// null si todavía no hay ninguna tarea que cuente.
        /// </summary>
        public decimal? calculada { get; set; }
        /// <summary>Nota que fijó el tutor; si existe, reemplaza a la calculada.</summary>
        public decimal? ajustada { get; set; }
        public decimal? final => ajustada ?? calculada;
        public string? comentario { get; set; }
        public DateTime? fechaAjuste { get; set; }

        /// <summary>Tareas que entran en el cálculo (calificadas o vencidas sin entregar).</summary>
        public int tareasConsideradas { get; set; }
        public int tareasTotales { get; set; }
        /// <summary>Entregadas pero todavía sin calificar.</summary>
        public int porCalificar { get; set; }
    }
}
