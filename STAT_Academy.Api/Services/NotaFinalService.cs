using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;

namespace STAT_Academy.Api.Services
{
    /// <summary>
    /// Ajustes de la nota final (tabla NOTA_FINAL). Si la tabla todavía no existe
    /// (falta aplicar 2026-11-15_nota_final.sql), las lecturas devuelven "sin ajustes"
    /// y <see cref="Disponible"/> queda en false para que la Web oculte el formulario.
    /// </summary>
    public class NotaFinalService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly ILogger<NotaFinalService> _logger;

        public NotaFinalService(ApplicationDbContext context, AuditoriaService auditoria, ILogger<NotaFinalService> logger)
        {
            _context = context;
            _auditoria = auditoria;
            _logger = logger;
        }

        /// <summary>False si la tabla NOTA_FINAL no existe (se sabe después de la primera lectura).</summary>
        public bool Disponible { get; private set; } = true;

        /// <summary>Ajustes por matrícula (id de matrícula → ajuste).</summary>
        public async Task<Dictionary<int, NotaFinalModel>> AjustesAsync(IReadOnlyCollection<int> matriculaIds)
        {
            if (matriculaIds.Count == 0)
                return [];

            try
            {
                return (await _context.NotaFinal
                        .AsNoTracking()
                        .Where(n => matriculaIds.Contains(n.fk_Matricula))
                        .ToListAsync())
                    .GroupBy(n => n.fk_Matricula)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(n => n.id).First());
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                // 208 = "Invalid object name": falta aplicar 2026-11-15_nota_final.sql.
                Disponible = false;
                _logger.LogWarning("No existe la tabla NOTA_FINAL; se muestra solo la nota calculada.");
                return [];
            }
        }

        public async Task<NotaFinalModel?> AjusteAsync(int matriculaId) =>
            (await AjustesAsync([matriculaId])).GetValueOrDefault(matriculaId);

        /// <summary>Guarda (o reemplaza) el ajuste. nota = null quita el ajuste.</summary>
        public async Task<(bool exitoso, string mensaje)> GuardarAsync(
            int matriculaId, decimal? nota, string? comentario, int tutorId, string tutorEmail, string estudiante)
        {
            try
            {
                var actual = await _context.NotaFinal.FirstOrDefaultAsync(n => n.fk_Matricula == matriculaId);

                if (nota == null)
                {
                    if (actual == null)
                        return (true, "La nota final ya era la calculada.");

                    _context.NotaFinal.Remove(actual);
                    await _context.SaveChangesAsync();

                    await _auditoria.RegistrarAsync("NOTA_FINAL", "DELETE",
                        $"Nota final de {Recortar(estudiante)} vuelve a la calculada", tutorEmail, actual.id);

                    return (true, "La nota final vuelve a ser la calculada a partir de las tareas.");
                }

                var corregida = actual != null;
                actual ??= _context.NotaFinal.Add(new NotaFinalModel { fk_Matricula = matriculaId }).Entity;

                actual.nota = Math.Round(nota.Value, 2);
                actual.comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
                actual.fk_Tutor = tutorId;
                actual.fecha_Edicion = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync("NOTA_FINAL", corregida ? "UPDATE" : "CREATE",
                    $"Nota final de {Recortar(estudiante)} ajustada a {actual.nota:0.##}", tutorEmail, actual.id);

                return (true, "Nota final ajustada.");
            }
            catch (Exception ex) when (EsTablaFaltante(ex))
            {
                Disponible = false;
                _logger.LogWarning("No existe la tabla NOTA_FINAL; no se puede guardar el ajuste.");
                return (false, "El ajuste de la nota final todavía no está habilitado en la base de datos.");
            }
        }

        private static bool EsTablaFaltante(Exception ex) =>
            ex is SqlException { Number: 208 }
            || ex is DbUpdateException { InnerException: SqlException { Number: 208 } };

        private static string Recortar(string texto) =>
            texto.Length > 80 ? texto[..80] + "…" : texto;
    }
}
