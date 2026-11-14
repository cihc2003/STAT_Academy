using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.DTOs.Compra;

namespace STAT_Academy.Api.Services
{
    /// <summary>
    /// Convierte el carrito del usuario en una compra: FACTURA + DETALLE_FACTURA,
    /// descuenta (reserva) el stock y vacía el carrito, todo en una sola transacción.
    /// La factura queda pendiente de pago: el usuario paga por SINPE Móvil o transferencia,
    /// sube el comprobante y un administrador lo aprueba (ver FacturaService).
    /// </summary>
    public class CompraService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly StockAlertaService _alertas;
        private readonly FacturaService _facturas;

        public CompraService(ApplicationDbContext context, AuditoriaService auditoria, StockAlertaService alertas, FacturaService facturas)
        {
            _context = context;
            _auditoria = auditoria;
            _alertas = alertas;
            _facturas = facturas;
        }

        public async Task<(CompraResponse? compra, string? error)> Comprar(int usuarioId, string usuarioEmail)
        {
            CompraResponse? compra = null;
            string? error = null;
            List<ProductoModel> cruzaronMinimo = [];

            // Con EnableRetryOnFailure, las transacciones manuales deben ir dentro de la estrategia
            // de ejecución para que un reintento repita la transacción completa.
            var estrategia = _context.Database.CreateExecutionStrategy();

            await estrategia.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();
                compra = null;
                error = null;
                cruzaronMinimo = [];

                await using var transaccion = await _context.Database.BeginTransactionAsync();

                var carrito = await _context.Carrito.FirstOrDefaultAsync(c => c.fk_Usuario == usuarioId);
                var lineas = carrito == null
                    ? []
                    : await _context.CarritoDetalle
                        .Where(d => d.fk_Carrito == carrito.id && d.fk_Producto != null && d.cantidad > 0)
                        .ToListAsync();

                if (lineas.Count == 0)
                {
                    error = "Tu carrito está vacío.";
                    return;
                }

                var ids = lineas.Select(l => l.fk_Producto!.Value).Distinct().ToList();
                var productos = await _context.Producto.AsNoTracking()
                    .Where(p => ids.Contains(p.id))
                    .ToDictionaryAsync(p => p.id);

                var detalles = new List<DetalleFacturaModel>();

                foreach (var linea in lineas)
                {
                    var productoId = linea.fk_Producto!.Value;

                    if (!productos.TryGetValue(productoId, out var producto) || !producto.estado)
                    {
                        error = $"Un producto de tu carrito ya no está disponible. Quítalo para continuar.";
                        return;
                    }

                    // Descuento atómico: solo se aplica si todavía hay existencias suficientes.
                    // Si otro cliente compró las últimas unidades al mismo tiempo, esta fila no cambia.
                    var actualizadas = await _context.Producto
                        .Where(p => p.id == productoId && p.estado && p.stock >= linea.cantidad)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.stock, p => p.stock - linea.cantidad));

                    if (actualizadas == 0)
                    {
                        var disponible = await _context.Producto.Where(p => p.id == productoId).Select(p => p.stock).FirstAsync();
                        error = disponible <= 0
                            ? $"\"{producto.nombre}\" se agotó. Quítalo del carrito para continuar."
                            : $"Solo quedan {disponible} unidades de \"{producto.nombre}\". Ajusta la cantidad para continuar.";
                        return; // la transacción se descarta: no se descuenta nada
                    }

                    var stockAntes = producto.stock;
                    var stockDespues = stockAntes - linea.cantidad;
                    if (stockAntes > producto.min_stock && stockDespues <= producto.min_stock)
                    {
                        producto.stock = stockDespues;
                        cruzaronMinimo.Add(producto);
                    }

                    detalles.Add(new DetalleFacturaModel
                    {
                        fk_Producto = productoId,
                        cantidad = linea.cantidad,
                        // Se guarda el precio del momento: si luego cambia, la factura no.
                        precio_Unitario = producto.precio_base,
                        subtotal = producto.precio_base * linea.cantidad
                    });
                }

                var total = detalles.Sum(d => d.subtotal ?? 0);
                var ahora = DateTime.UtcNow;

                // Sin PAGO todavía: se crea cuando un administrador aprueba el comprobante.
                var factura = new FacturaModel
                {
                    fk_Usuario = usuarioId,
                    fecha = ahora,
                    total = total,
                    pagada = false,
                    Detalles = detalles
                };
                _context.Factura.Add(factura);

                _context.CarritoDetalle.RemoveRange(lineas);
                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();

                compra = new CompraResponse
                {
                    facturaId = factura.id,
                    fecha = DateTime.SpecifyKind(ahora, DateTimeKind.Utc),
                    total = total,
                    pagada = false,
                    lineas = detalles.Select(d => new CompraLineaResponse
                    {
                        productoId = d.fk_Producto,
                        nombre = productos[d.fk_Producto!.Value].nombre,
                        cantidad = d.cantidad ?? 0,
                        precio_unitario = d.precio_Unitario ?? 0,
                        subtotal = d.subtotal ?? 0
                    }).ToList()
                };
            });

            if (compra == null)
                return (null, error ?? "No se pudo completar la compra.");

            _auditoria.Registrar("FACTURA", "CREATE",
                $"Compra de {compra.lineas.Sum(l => l.cantidad)} artículo(s) por {compra.total:0.00}", usuarioEmail, compra.facturaId);

            await _alertas.Notificar(cruzaronMinimo);
            await _facturas.EnviarComprobanteDeCompra(compra.facturaId);

            return (compra, null);
        }

        /// <summary>
        /// Compra de un curso de pago: crea la factura pendiente con una línea del curso.
        /// El acceso se habilita cuando un administrador aprueba el pago.
        /// Si ya hay una factura sin pagar de ese curso, se devuelve esa en lugar de crear otra.
        /// </summary>
        public async Task<(int? facturaId, string? error, bool existente)> ComprarCurso(int usuarioId, string usuarioEmail, int cursoId)
        {
            var curso = await _context.Curso.AsNoTracking().FirstOrDefaultAsync(c => c.id == cursoId && c.estado);
            if (curso == null)
                return (null, "El curso no existe o no está disponible.", false);

            if (curso.precio <= 0)
                return (null, "Este curso es gratuito: puedes inscribirte directamente.", false);

            var yaInscrito = await _context.EstudianteCurso
                .AnyAsync(m => m.EstudianteId == usuarioId && m.CursoId == cursoId && m.Estado == "Activo");
            if (yaInscrito)
                return (null, "Ya estás inscrito en este curso.", false);

            var pendiente = await _context.Factura.AsNoTracking()
                .Where(f => f.fk_Usuario == usuarioId && f.pagada != true
                            && f.Detalles.Any(d => d.fk_Curso == cursoId))
                .OrderByDescending(f => f.id)
                .Select(f => (int?)f.id)
                .FirstOrDefaultAsync();
            if (pendiente != null)
                return (pendiente, null, true);

            var factura = new FacturaModel
            {
                fk_Usuario = usuarioId,
                fecha = DateTime.UtcNow,
                total = curso.precio,
                pagada = false,
                Detalles =
                [
                    new DetalleFacturaModel
                    {
                        fk_Curso = cursoId,
                        cantidad = 1,
                        precio_Unitario = curso.precio,
                        subtotal = curso.precio
                    }
                ]
            };

            _context.Factura.Add(factura);
            await _context.SaveChangesAsync();

            _auditoria.Registrar("FACTURA", "CREATE",
                $"Compra del curso \"{curso.nombre}\" por {curso.precio:0.00}", usuarioEmail, factura.id);

            await _facturas.EnviarComprobanteDeCompra(factura.id);

            return (factura.id, null, false);
        }

        /// <summary>Una compra del usuario (o cualquiera, si es Admin).</summary>
        public CompraResponse? ObtenerCompra(int facturaId, int usuarioId, bool esAdmin)
        {
            var factura = _context.Factura
                .AsNoTracking()
                .Include(f => f.Detalles)
                .FirstOrDefault(f => f.id == facturaId && (esAdmin || f.fk_Usuario == usuarioId));

            if (factura == null)
                return null;

            var ids = factura.Detalles.Where(d => d.fk_Producto != null).Select(d => d.fk_Producto!.Value).ToList();
            var nombres = _context.Producto.Where(p => ids.Contains(p.id)).ToDictionary(p => p.id, p => p.nombre);

            return new CompraResponse
            {
                facturaId = factura.id,
                fecha = DateTime.SpecifyKind(factura.fecha ?? DateTime.UtcNow, DateTimeKind.Utc),
                total = factura.total ?? 0,
                pagada = factura.pagada == true,
                lineas = factura.Detalles.Select(d => new CompraLineaResponse
                {
                    productoId = d.fk_Producto,
                    nombre = d.fk_Producto != null && nombres.TryGetValue(d.fk_Producto.Value, out var n) ? n : "Producto",
                    cantidad = d.cantidad ?? 0,
                    precio_unitario = d.precio_Unitario ?? 0,
                    subtotal = d.subtotal ?? 0
                }).ToList()
            };
        }
    }
}
