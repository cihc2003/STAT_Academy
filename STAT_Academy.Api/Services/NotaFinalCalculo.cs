using STAT_Academy.Api.Models;
using STAT_Academy.DTOs.Aula;

namespace STAT_Academy.Api.Services
{
    /// <summary>
    /// Nota final de un estudiante en un curso (sobre 100). Misma regla para el aula y el tutor:
    /// <list type="bullet">
    /// <item>Tarea calificada: cuenta su nota.</item>
    /// <item>Tarea vencida (fecha límite pasada, hora de Costa Rica) sin entregar: cuenta 0.</item>
    /// <item>Tarea abierta, o entregada y todavía sin calificar: no cuenta aún.</item>
    /// </list>
    /// Si el tutor guardó un ajuste (NOTA_FINAL), ese valor reemplaza al calculado.
    /// </summary>
    public static class NotaFinalCalculo
    {
        public static NotaFinalResponse Calcular(
            IReadOnlyCollection<TareaModel> tareasActivas,
            IEnumerable<EntregaTareaModel> entregasDelEstudiante,
            NotaFinalModel? ajuste,
            DateTime? ahora = null)
        {
            var momento = ahora ?? CursoModel.AhoraCostaRica;
            var porTarea = entregasDelEstudiante
                .Where(e => e.fk_Tarea != null)
                .GroupBy(e => e.fk_Tarea!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.id).First());

            var notas = new List<decimal>();
            var porCalificar = 0;

            foreach (var tarea in tareasActivas)
            {
                porTarea.TryGetValue(tarea.ID, out var entrega);

                if (entrega?.calificacion is decimal nota)
                    notas.Add(nota);
                else if (entrega?.fecha_Entrega != null)
                    porCalificar++;
                else if (Vencida(tarea, momento))
                    notas.Add(0);
            }

            return new NotaFinalResponse
            {
                calculada = notas.Count > 0 ? Math.Round(notas.Average(), 2) : null,
                ajustada = ajuste?.nota,
                comentario = ajuste?.comentario,
                fechaAjuste = ajuste?.fecha_Edicion,
                tareasConsideradas = notas.Count,
                tareasTotales = tareasActivas.Count,
                porCalificar = porCalificar
            };
        }

        public static bool Vencida(TareaModel tarea, DateTime? ahora = null) =>
            tarea.Fecha_Limite != null && tarea.Fecha_Limite.Value < (ahora ?? CursoModel.AhoraCostaRica);
    }
}
