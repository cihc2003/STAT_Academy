using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;

namespace STAT_Academy.Api.Services
{
    /// <summary>
    /// Avisa a los administradores cuando un producto queda en o por debajo de su stock mínimo.
    /// Se llama solo cuando el producto CRUZA el umbral (no en cada venta posterior),
    /// para no llenar el buzón con el mismo aviso.
    /// </summary>
    public class StockAlertaService
    {
        private readonly ApplicationDbContext _context;
        private readonly CorreoService _correo;
        private readonly AuditoriaService _auditoria;
        private readonly ILogger<StockAlertaService> _logger;

        public StockAlertaService(
            ApplicationDbContext context,
            CorreoService correo,
            AuditoriaService auditoria,
            ILogger<StockAlertaService> logger)
        {
            _context = context;
            _correo = correo;
            _auditoria = auditoria;
            _logger = logger;
        }

        public async Task Notificar(IReadOnlyCollection<ProductoModel> productos)
        {
            if (productos.Count == 0)
                return;

            foreach (var p in productos)
            {
                _auditoria.Registrar("PRODUCTO", "STOCK_BAJO",
                    $"Stock bajo: {p.nombre} tiene {p.stock} (mínimo {p.min_stock})", "sistema", p.id);
            }

            var administradores = _context.Usuario
                .Where(u => u.fk_Tipo_Usuario == 1 && u.estado && u.email != null)
                .Select(u => u.email!)
                .ToList();

            if (administradores.Count == 0)
            {
                _logger.LogWarning("Stock bajo en {Cantidad} producto(s), pero no hay administradores activos a quienes avisar.", productos.Count);
                return;
            }

            try
            {
                await _correo.EnviarAlertaStockBajo(
                    administradores,
                    productos.Select(p => (p.nombre, p.stock, p.min_stock)).ToList());
            }
            catch (Exception ex)
            {
                // El aviso por correo es complementario: el panel de administración también lo muestra.
                _logger.LogError(ex, "No se pudo enviar el aviso de stock bajo.");
            }
        }
    }
}
