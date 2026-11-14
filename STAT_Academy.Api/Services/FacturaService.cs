using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using STAT_Academy.Api.Configuration;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Storage;
using STAT_Academy.DTOs.Factura;

namespace STAT_Academy.Api.Services
{
    /// <summary>
    /// Facturas y pagos por SINPE Móvil o transferencia.
    ///
    /// Flujo:
    ///  1. La compra crea la FACTURA (Pago = 0) y se envía el comprobante de compra por correo.
    ///  2. El usuario paga y sube el comprobante: se crea un PEDIDO_PAGO en PENDIENTE.
    ///  3. Un Admin lo aprueba (PAGO confirmado, FACTURA pagada, matrícula en los cursos)
    ///     o lo rechaza (con motivo; el usuario puede enviar otro). Ambos avisan por correo.
    /// </summary>
    public class FacturaService
    {
        /// <summary>Tipos aceptados para el comprobante (subconjunto de BlogArchivoValidador).</summary>
        private static readonly HashSet<string> ExtensionesComprobante =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };

        public const string ExtensionesComprobanteTexto = ".jpg, .png, .webp, .pdf";

        /// <summary>Tamaño máximo de la petición de subida (un archivo de hasta 10 MB + margen).</summary>
        public const long MaxBytesPeticion = 11 * 1024 * 1024;

        private readonly ApplicationDbContext _context;
        private readonly IBlobStorageService _blob;
        private readonly CorreoService _correo;
        private readonly AuditoriaService _auditoria;
        private readonly PagosOptions _pagos;
        private readonly ILogger<FacturaService> _logger;

        public FacturaService(
            ApplicationDbContext context,
            IBlobStorageService blob,
            CorreoService correo,
            AuditoriaService auditoria,
            IOptions<PagosOptions> pagos,
            ILogger<FacturaService> logger)
        {
            _context = context;
            _blob = blob;
            _correo = correo;
            _auditoria = auditoria;
            _pagos = pagos.Value;
            _logger = logger;
        }

        public InstruccionesPagoResponse Instrucciones() => new()
        {
            sinpeTelefono = _pagos.SinpeTelefono,
            sinpeNombre = _pagos.SinpeNombre,
            banco = _pagos.Banco,
            cuentaIban = _pagos.CuentaIban,
            cuentaTitular = _pagos.CuentaTitular,
            cedulaJuridica = string.IsNullOrWhiteSpace(_pagos.CedulaJuridica) ? null : _pagos.CedulaJuridica
        };

        // ---------------------------------------------------------------
        // Consultas
        // ---------------------------------------------------------------

        /// <summary>Facturas de un usuario (las más recientes primero).</summary>
        public Task<List<FacturaResumenResponse>> MisFacturas(int usuarioId) =>
            Resumenes(_context.Factura.Where(f => f.fk_Usuario == usuarioId));

        /// <summary>Todas las facturas (Admin).</summary>
        public Task<List<FacturaResumenResponse>> Todas() =>
            Resumenes(_context.Factura);

        /// <summary>Comprobantes de todos los usuarios (Admin), opcionalmente por estado.</summary>
        public async Task<List<PagoComprobanteResponse>> Pagos(string? estado)
        {
            var consulta = _context.PedidoPago.AsNoTracking().Where(p => p.fk_Factura != null);

            if (!string.IsNullOrWhiteSpace(estado))
            {
                var e = estado.Trim().ToUpperInvariant();
                consulta = consulta.Where(p => p.estado == e);
            }

            var pagos = await consulta.OrderByDescending(p => p.fechaCreacion).ToListAsync();
            return await APagosResponse(pagos);
        }

        /// <summary>Cuántos comprobantes esperan revisión (para el panel de control).</summary>
        public Task<int> PendientesDeRevision() =>
            _context.PedidoPago.CountAsync(p => p.fk_Factura != null && p.estado == PedidoPagoModel.Pendiente);

        /// <summary>Detalle de una factura propia (o de cualquiera si es Admin). Null si no existe o no es suya.</summary>
        public async Task<FacturaDetalleResponse?> Detalle(int facturaId, int usuarioId, bool esAdmin)
        {
            var factura = await _context.Factura.AsNoTracking()
                .Include(f => f.Detalles)
                .FirstOrDefaultAsync(f => f.id == facturaId && (esAdmin || f.fk_Usuario == usuarioId));

            if (factura == null)
                return null;

            var usuario = await _context.Usuario.AsNoTracking()
                .Where(u => u.id == factura.fk_Usuario)
                .Select(u => new { u.nombre, u.email })
                .FirstOrDefaultAsync();

            var pedidos = await _context.PedidoPago.AsNoTracking()
                .Where(p => p.fk_Factura == facturaId)
                .OrderByDescending(p => p.fechaCreacion)
                .ToListAsync();

            var lineas = await Lineas(factura.Detalles);
            var pagada = factura.pagada == true;

            return new FacturaDetalleResponse
            {
                id = factura.id,
                fecha = Utc(factura.fecha),
                total = factura.total ?? 0,
                pagada = pagada,
                estado = Estado(pagada, pedidos.FirstOrDefault()?.estado),
                descripcion = Descripcion(lineas),
                usuarioId = factura.fk_Usuario ?? 0,
                usuarioNombre = usuario?.nombre ?? "",
                usuarioEmail = usuario?.email ?? "",
                cantidadComprobantes = pedidos.Count,
                lineas = lineas,
                pagos = await APagosResponse(pedidos),
                puedeEnviarComprobante = !pagada && (factura.total ?? 0) > 0
                                         && pedidos.All(p => p.estado != PedidoPagoModel.Pendiente),
                instrucciones = Instrucciones()
            };
        }

        // ---------------------------------------------------------------
        // Comprobante de compra (correo)
        // ---------------------------------------------------------------

        /// <summary>
        /// Envía al comprador el comprobante de su compra con los datos para pagar.
        /// Un fallo del correo no deshace la compra: la factura queda en "Mis facturas y pagos".
        /// </summary>
        public async Task EnviarComprobanteDeCompra(int facturaId)
        {
            try
            {
                var detalle = await Detalle(facturaId, 0, esAdmin: true);
                if (detalle == null || string.IsNullOrWhiteSpace(detalle.usuarioEmail))
                    return;

                await _correo.EnviarCompraRegistrada(detalle.usuarioEmail, detalle.usuarioNombre, detalle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el comprobante de la compra {FacturaId}.", facturaId);
            }
        }

        // ---------------------------------------------------------------
        // Envío del comprobante de pago (usuario)
        // ---------------------------------------------------------------

        public async Task<(PagoComprobanteResponse? pago, string? error, bool noExiste)> EnviarComprobante(
            int facturaId, int usuarioId, string usuarioEmail,
            string? metodo, string? referencia, IFormFile? archivo, CancellationToken ct)
        {
            var factura = await _context.Factura.AsNoTracking()
                .FirstOrDefaultAsync(f => f.id == facturaId && f.fk_Usuario == usuarioId, ct);

            if (factura == null)
                return (null, "La factura no existe.", true);

            if (factura.pagada == true)
                return (null, "Esta factura ya está pagada.", false);

            var monto = factura.total ?? 0;
            if (monto <= 0)
                return (null, "Esta factura no tiene un monto por pagar.", false);

            if (await _context.PedidoPago.AnyAsync(p => p.fk_Factura == facturaId && p.estado == PedidoPagoModel.Pendiente, ct))
                return (null, "Ya enviaste un comprobante para esta compra y está en verificación.", false);

            var metodoNormalizado = (metodo ?? "").Trim().ToUpperInvariant();
            if (metodoNormalizado is not (PedidoPagoModel.MetodoSinpe or PedidoPagoModel.MetodoTransferencia))
                return (null, "Selecciona cómo pagaste: SINPE Móvil o transferencia.", false);

            referencia = string.IsNullOrWhiteSpace(referencia) ? null : referencia.Trim();
            if (referencia is { Length: > 100 })
                return (null, "El número de referencia admite hasta 100 caracteres.", false);

            if (archivo == null || archivo.Length == 0)
                return (null, "Adjunta el comprobante de pago.", false);

            if (!ExtensionesComprobante.Contains(Path.GetExtension(archivo.FileName)))
                return (null, $"El comprobante debe ser una imagen o un PDF ({ExtensionesComprobanteTexto}).", false);

            // Extensión, tamaño y firma real del contenido.
            var (valido, errorArchivo) = await BlogArchivoValidador.Validar(archivo);
            if (valido == null)
                return (null, errorArchivo, false);

            // Siempre al contenedor PRIVADO: un comprobante tiene datos bancarios del usuario.
            string blobName;
            try
            {
                blobName = await _blob.SubirPrivadoAsync(valido.Contenido, valido.Extension, valido.Mime, $"comprobantes/{facturaId}", ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "No se pudo subir el comprobante de la factura {FacturaId}.", facturaId);
                return (null, "No se pudo guardar el comprobante. Inténtalo de nuevo en unos minutos.", false);
            }

            var pedido = new PedidoPagoModel
            {
                fk_Usuario = usuarioId,
                fk_Factura = facturaId,
                metodo = metodoNormalizado,
                estado = PedidoPagoModel.Pendiente,
                monto = monto,
                comprobanteBlob = blobName,
                referencia = referencia,
                fechaCreacion = DateTime.UtcNow
            };

            try
            {
                _context.PedidoPago.Add(pedido);
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // P. ej. dos envíos simultáneos: el índice único deja pasar solo uno.
                _logger.LogWarning(ex, "No se registró el comprobante de la factura {FacturaId}.", facturaId);
                await BorrarBlob(blobName);
                return (null, "No se pudo registrar el comprobante. Revisa si ya hay uno en verificación.", false);
            }

            _auditoria.Registrar("PEDIDO_PAGO", "CREATE",
                $"Comprobante {metodoNormalizado} por {monto:0.00} para la factura #{facturaId}", usuarioEmail, pedido.id);

            await AvisarAdministradores(facturaId, usuarioId, monto, metodoNormalizado);

            return ((await APagosResponse([pedido])).First(), null, false);
        }

        /// <summary>Archivo del comprobante: solo el dueño o un Admin.</summary>
        public async Task<(ArchivoDescarga contenido, string nombre)?> AbrirComprobante(
            int pagoId, int usuarioId, bool esAdmin, CancellationToken ct)
        {
            var pedido = await _context.PedidoPago.AsNoTracking()
                .FirstOrDefaultAsync(p => p.id == pagoId && (esAdmin || p.fk_Usuario == usuarioId), ct);

            if (pedido == null || string.IsNullOrWhiteSpace(pedido.comprobanteBlob))
                return null;

            try
            {
                var descarga = await _blob.DescargarAsync(ContenedorBlob.Documentos, pedido.comprobanteBlob, ct);
                var nombre = $"comprobante-compra-{pedido.fk_Factura}-{pedido.id}{Path.GetExtension(pedido.comprobanteBlob)}";
                return (descarga, nombre);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("El comprobante {PagoId} no existe en Blob Storage.", pagoId);
                return null;
            }
        }

        // ---------------------------------------------------------------
        // Revisión (Admin)
        // ---------------------------------------------------------------

        public async Task<(bool ok, string mensaje)> Aprobar(int pagoId, int adminId, string adminEmail)
        {
            PedidoPagoModel? pedido = null;
            FacturaModel? factura = null;
            List<string> cursosHabilitados = [];
            string? error = null;

            var estrategia = _context.Database.CreateExecutionStrategy();

            await estrategia.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();
                pedido = null;
                factura = null;
                cursosHabilitados = [];
                error = null;

                await using var transaccion = await _context.Database.BeginTransactionAsync();
                var ahora = DateTime.UtcNow;

                // Cambio atómico: solo se aprueba si sigue PENDIENTE (dos admins a la vez no lo aprueban dos veces).
                var cambiados = await _context.PedidoPago
                    .Where(p => p.id == pagoId && p.fk_Factura != null && p.estado == PedidoPagoModel.Pendiente)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.estado, PedidoPagoModel.Aprobado)
                        .SetProperty(p => p.fechaRevision, ahora)
                        .SetProperty(p => p.fk_Admin, adminId));

                if (cambiados == 0)
                {
                    error = "El comprobante no existe o ya fue revisado.";
                    return;
                }

                pedido = await _context.PedidoPago.AsNoTracking().FirstAsync(p => p.id == pagoId);
                factura = await _context.Factura.Include(f => f.Detalles).FirstOrDefaultAsync(f => f.id == pedido.fk_Factura);

                if (factura == null)
                {
                    error = "La factura del comprobante no existe.";
                    return;
                }

                if (factura.pagada == true)
                {
                    error = "Esta factura ya estaba pagada.";
                    return; // se descarta la transacción
                }

                var pago = new PagoModel
                {
                    fk_Usuario = factura.fk_Usuario,
                    fk_Metodo_Pago = await MetodoPagoId(pedido.metodo),
                    monto = pedido.monto,
                    fecha_Pago = ahora,
                    estado = true
                };
                _context.Pago.Add(pago);
                await _context.SaveChangesAsync();

                factura.fk_Pago = pago.id;
                factura.pagada = true;

                // Cursos comprados: se habilita el acceso (matrícula activa).
                var cursoIds = factura.Detalles.Where(d => d.fk_Curso != null).Select(d => d.fk_Curso!.Value).Distinct().ToList();
                if (cursoIds.Count > 0 && factura.fk_Usuario is int estudianteId)
                {
                    var existentes = await _context.EstudianteCurso
                        .Where(m => m.EstudianteId == estudianteId && cursoIds.Contains(m.CursoId))
                        .ToListAsync();

                    foreach (var cursoId in cursoIds)
                    {
                        var matricula = existentes.FirstOrDefault(m => m.CursoId == cursoId);
                        if (matricula == null)
                        {
                            _context.EstudianteCurso.Add(new EstudianteCursoModel
                            {
                                CursoId = cursoId,
                                EstudianteId = estudianteId,
                                Fecha_Matricula = ahora,
                                Estado = "Activo",
                                Progreso = 0
                            });
                        }
                        else
                        {
                            matricula.Estado = "Activo";
                        }
                    }

                    cursosHabilitados = await _context.Curso
                        .Where(c => cursoIds.Contains(c.id))
                        .Select(c => c.nombre)
                        .ToListAsync();
                }

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();
            });

            if (error != null || pedido == null || factura == null)
                return (false, error ?? "No se pudo aprobar el pago.");

            _auditoria.Registrar("PEDIDO_PAGO", "APROBAR",
                $"Pago aprobado de la factura #{factura.id} por {pedido.monto:0.00}", adminEmail, pedido.id);

            var usuario = await DatosUsuario(factura.fk_Usuario);
            if (usuario != null)
            {
                try
                {
                    await _correo.EnviarPagoAprobado(usuario.Value.email, usuario.Value.nombre, factura.id, pedido.monto, cursosHabilitados);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudo avisar la aprobación del pago {PagoId}.", pagoId);
                    return (true, "Pago aprobado, pero no se pudo enviar el correo al usuario.");
                }
            }

            return (true, cursosHabilitados.Count > 0
                ? "Pago aprobado. El usuario ya tiene acceso a los cursos comprados."
                : "Pago aprobado y usuario notificado.");
        }

        public async Task<(bool ok, string mensaje)> Rechazar(int pagoId, int adminId, string adminEmail, string? observacion)
        {
            observacion = observacion?.Trim();
            if (string.IsNullOrWhiteSpace(observacion))
                return (false, "Indica el motivo del rechazo: se le enviará al usuario.");

            if (observacion.Length > 500)
                return (false, "El motivo admite hasta 500 caracteres.");

            var ahora = DateTime.UtcNow;
            var cambiados = await _context.PedidoPago
                .Where(p => p.id == pagoId && p.fk_Factura != null && p.estado == PedidoPagoModel.Pendiente)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.estado, PedidoPagoModel.Rechazado)
                    .SetProperty(p => p.fechaRevision, ahora)
                    .SetProperty(p => p.fk_Admin, adminId)
                    .SetProperty(p => p.observacion, observacion));

            if (cambiados == 0)
                return (false, "El comprobante no existe o ya fue revisado.");

            var pedido = await _context.PedidoPago.AsNoTracking().FirstAsync(p => p.id == pagoId);

            _auditoria.Registrar("PEDIDO_PAGO", "RECHAZAR",
                $"Pago rechazado de la factura #{pedido.fk_Factura}: {observacion}", adminEmail, pedido.id);

            var usuario = await DatosUsuario(pedido.fk_Usuario);
            if (usuario != null)
            {
                try
                {
                    await _correo.EnviarPagoRechazado(usuario.Value.email, usuario.Value.nombre, pedido.fk_Factura ?? 0, observacion);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudo avisar el rechazo del pago {PagoId}.", pagoId);
                    return (true, "Pago rechazado, pero no se pudo enviar el correo al usuario.");
                }
            }

            return (true, "Pago rechazado y usuario notificado.");
        }

        // ---------------------------------------------------------------
        // Ayudantes
        // ---------------------------------------------------------------

        private async Task<List<FacturaResumenResponse>> Resumenes(IQueryable<FacturaModel> consulta)
        {
            var facturas = await consulta.AsNoTracking()
                .Include(f => f.Detalles)
                .OrderByDescending(f => f.fecha).ThenByDescending(f => f.id)
                .ToListAsync();

            if (facturas.Count == 0)
                return [];

            var ids = facturas.Select(f => f.id).ToList();
            var usuarioIds = facturas.Where(f => f.fk_Usuario != null).Select(f => f.fk_Usuario!.Value).Distinct().ToList();

            var usuarios = await _context.Usuario.AsNoTracking()
                .Where(u => usuarioIds.Contains(u.id))
                .ToDictionaryAsync(u => u.id, u => new { u.nombre, u.email });

            var pedidos = await _context.PedidoPago.AsNoTracking()
                .Where(p => p.fk_Factura != null && ids.Contains(p.fk_Factura.Value))
                .Select(p => new { p.fk_Factura, p.estado, p.fechaCreacion, p.id })
                .ToListAsync();

            var nombres = await NombresDe(facturas.SelectMany(f => f.Detalles));

            return facturas.Select(f =>
            {
                var propios = pedidos.Where(p => p.fk_Factura == f.id)
                    .OrderByDescending(p => p.fechaCreacion).ThenByDescending(p => p.id).ToList();
                var pagada = f.pagada == true;
                var usuario = f.fk_Usuario is int uid && usuarios.TryGetValue(uid, out var u) ? u : null;

                return new FacturaResumenResponse
                {
                    id = f.id,
                    fecha = Utc(f.fecha),
                    total = f.total ?? 0,
                    pagada = pagada,
                    estado = Estado(pagada, propios.FirstOrDefault()?.estado),
                    descripcion = Descripcion(f.Detalles.Select(d => ALinea(d, nombres)).ToList()),
                    usuarioId = f.fk_Usuario ?? 0,
                    usuarioNombre = usuario?.nombre ?? "",
                    usuarioEmail = usuario?.email ?? "",
                    cantidadComprobantes = propios.Count
                };
            }).ToList();
        }

        private async Task<List<FacturaLineaResponse>> Lineas(IEnumerable<DetalleFacturaModel> detalles)
        {
            var lista = detalles.ToList();
            var nombres = await NombresDe(lista);
            return lista.Select(d => ALinea(d, nombres)).ToList();
        }

        private sealed record Nombres(Dictionary<int, string> Productos, Dictionary<int, string> Cursos);

        private async Task<Nombres> NombresDe(IEnumerable<DetalleFacturaModel> detalles)
        {
            var lista = detalles.ToList();
            var productoIds = lista.Where(d => d.fk_Producto != null).Select(d => d.fk_Producto!.Value).Distinct().ToList();
            var cursoIds = lista.Where(d => d.fk_Curso != null).Select(d => d.fk_Curso!.Value).Distinct().ToList();

            var productos = productoIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Producto.AsNoTracking().Where(p => productoIds.Contains(p.id)).ToDictionaryAsync(p => p.id, p => p.nombre);

            var cursos = cursoIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Curso.AsNoTracking().Where(c => cursoIds.Contains(c.id)).ToDictionaryAsync(c => c.id, c => c.nombre);

            return new Nombres(productos, cursos);
        }

        private static FacturaLineaResponse ALinea(DetalleFacturaModel d, Nombres nombres)
        {
            var esCurso = d.fk_Curso != null;
            var nombre = esCurso
                ? (nombres.Cursos.TryGetValue(d.fk_Curso!.Value, out var c) ? $"Curso: {c}" : "Curso")
                : (d.fk_Producto != null && nombres.Productos.TryGetValue(d.fk_Producto.Value, out var p) ? p : "Producto");

            return new FacturaLineaResponse
            {
                tipo = esCurso ? "CURSO" : "PRODUCTO",
                productoId = d.fk_Producto,
                cursoId = d.fk_Curso,
                nombre = nombre,
                cantidad = d.cantidad ?? 0,
                precio_unitario = d.precio_Unitario ?? 0,
                subtotal = d.subtotal ?? 0
            };
        }

        private static string Descripcion(List<FacturaLineaResponse> lineas)
        {
            if (lineas.Count == 1 && lineas[0].tipo == "CURSO")
                return lineas[0].nombre;

            if (lineas.Count == 1)
                return lineas[0].cantidad == 1 ? lineas[0].nombre : $"{lineas[0].nombre} × {lineas[0].cantidad}";

            var unidades = lineas.Sum(l => l.cantidad);
            return $"{unidades} artículo{(unidades == 1 ? "" : "s")}";
        }

        /// <summary>Estado visible de la factura según si está pagada y el último comprobante.</summary>
        private static string Estado(bool pagada, string? ultimoComprobante) =>
            pagada ? EstadoFactura.Pagada
            : ultimoComprobante switch
            {
                PedidoPagoModel.Pendiente => EstadoFactura.EnVerificacion,
                PedidoPagoModel.Rechazado => EstadoFactura.Rechazada,
                _ => EstadoFactura.PendientePago
            };

        private async Task<List<PagoComprobanteResponse>> APagosResponse(List<PedidoPagoModel> pedidos)
        {
            if (pedidos.Count == 0)
                return [];

            var ids = pedidos.Select(p => p.fk_Usuario)
                .Concat(pedidos.Where(p => p.fk_Admin != null).Select(p => p.fk_Admin!.Value))
                .Distinct().ToList();

            var usuarios = await _context.Usuario.AsNoTracking()
                .Where(u => ids.Contains(u.id))
                .ToDictionaryAsync(u => u.id, u => new { u.nombre, u.email });

            return pedidos.Select(p => new PagoComprobanteResponse
            {
                id = p.id,
                facturaId = p.fk_Factura,
                metodo = p.metodo,
                estado = p.estado,
                monto = p.monto,
                referencia = p.referencia,
                fechaEnvio = Utc(p.fechaCreacion),
                fechaRevision = p.fechaRevision is DateTime r ? Utc(r) : null,
                observacion = p.observacion,
                revisadoPor = p.fk_Admin is int a && usuarios.TryGetValue(a, out var admin) ? admin.nombre : null,
                esImagen = Path.GetExtension(p.comprobanteBlob).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".webp",
                usuarioId = p.fk_Usuario,
                usuarioNombre = usuarios.TryGetValue(p.fk_Usuario, out var u) ? u.nombre ?? "" : "",
                usuarioEmail = usuarios.TryGetValue(p.fk_Usuario, out var u2) ? u2.email ?? "" : ""
            }).ToList();
        }

        /// <summary>Id en METODO_PAGO para el método, si el catálogo lo tiene (si no, queda null).</summary>
        private async Task<int?> MetodoPagoId(string metodo)
        {
            var clave = metodo == PedidoPagoModel.MetodoSinpe ? "SINPE" : "TRANSFER";
            return await _context.MetodoPago.AsNoTracking()
                .Where(m => m.nombre != null && m.nombre.ToUpper().Contains(clave))
                .Select(m => (int?)m.id)
                .FirstOrDefaultAsync();
        }

        private async Task<(string email, string nombre)?> DatosUsuario(int? usuarioId)
        {
            if (usuarioId == null)
                return null;

            var u = await _context.Usuario.AsNoTracking()
                .Where(x => x.id == usuarioId && x.email != null)
                .Select(x => new { x.email, x.nombre })
                .FirstOrDefaultAsync();

            return u == null ? null : (u.email!, u.nombre ?? "");
        }

        private async Task AvisarAdministradores(int facturaId, int usuarioId, decimal monto, string metodo)
        {
            try
            {
                var administradores = await _context.Usuario.AsNoTracking()
                    .Where(u => u.fk_Tipo_Usuario == 1 && u.estado && u.email != null)
                    .Select(u => u.email!)
                    .ToListAsync();

                if (administradores.Count == 0)
                    return;

                var nombre = (await DatosUsuario(usuarioId))?.nombre ?? "Un usuario";
                await _correo.EnviarComprobanteRecibido(administradores, facturaId, nombre, monto, metodo);
            }
            catch (Exception ex)
            {
                // Complementario: el panel de administración también muestra los pendientes.
                _logger.LogError(ex, "No se pudo avisar a los administradores del comprobante de la factura {FacturaId}.", facturaId);
            }
        }

        private async Task BorrarBlob(string blobName)
        {
            try
            {
                await _blob.EliminarAsync(ContenedorBlob.Documentos, blobName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo borrar {Blob} de Blob Storage.", blobName);
            }
        }

        private static DateTime Utc(DateTime? fecha) =>
            DateTime.SpecifyKind(fecha ?? DateTime.UtcNow, DateTimeKind.Utc);
    }
}
