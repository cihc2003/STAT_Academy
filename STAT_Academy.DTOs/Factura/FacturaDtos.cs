namespace STAT_Academy.DTOs.Factura
{
    /// <summary>Estados de una factura según su pago (calculados, no hay columna).</summary>
    public static class EstadoFactura
    {
        /// <summary>Todavía no se ha enviado ningún comprobante.</summary>
        public const string PendientePago = "PENDIENTE_PAGO";
        /// <summary>Hay un comprobante esperando la revisión del administrador.</summary>
        public const string EnVerificacion = "EN_VERIFICACION";
        /// <summary>El último comprobante fue rechazado; el usuario puede enviar otro.</summary>
        public const string Rechazada = "RECHAZADA";
        /// <summary>El pago fue aprobado.</summary>
        public const string Pagada = "PAGADA";
    }

    public class FacturaResumenResponse
    {
        public int id { get; set; }
        public DateTime fecha { get; set; }
        public decimal total { get; set; }
        public bool pagada { get; set; }
        public string estado { get; set; } = EstadoFactura.PendientePago;
        /// <summary>Texto corto de lo comprado: "Curso X" o "3 artículos".</summary>
        public string descripcion { get; set; } = "";
        public int usuarioId { get; set; }
        public string usuarioNombre { get; set; } = "";
        public string usuarioEmail { get; set; } = "";
        public int cantidadComprobantes { get; set; }
    }

    /// <summary>Órdenes de la lista paginada de facturas del usuario.</summary>
    public static class OrdenFacturas
    {
        public const string Recientes = "recientes";
        public const string Antiguas = "antiguas";
        public const string TotalMayor = "total_desc";
        public const string TotalMenor = "total";

        public static readonly string[] Todos = [Recientes, Antiguas, TotalMayor, TotalMenor];

        /// <summary>Devuelve el orden pedido si es válido; si no, "recientes".</summary>
        public static string Normalizar(string? orden)
        {
            var o = orden?.Trim().ToLowerInvariant();
            return o != null && Array.IndexOf(Todos, o) >= 0 ? o : Recientes;
        }
    }

    /// <summary>Una página de las facturas del usuario.</summary>
    public class FacturaPaginaResponse
    {
        public List<FacturaResumenResponse> items { get; set; } = new();
        public int pagina { get; set; }
        public int tamano { get; set; }
        /// <summary>Total de facturas del usuario.</summary>
        public int total { get; set; }
        public int totalPaginas { get; set; }
        /// <summary>Orden aplicado (ver <see cref="OrdenFacturas"/>).</summary>
        public string orden { get; set; } = OrdenFacturas.Recientes;
        /// <summary>Facturas sin pagar y sin comprobante en revisión (en todas las páginas).</summary>
        public int porPagar { get; set; }
    }

    public class FacturaDetalleResponse : FacturaResumenResponse
    {
        public List<FacturaLineaResponse> lineas { get; set; } = new();
        public List<PagoComprobanteResponse> pagos { get; set; } = new();
        /// <summary>True si el dueño puede enviar un comprobante (no pagada y nada en verificación).</summary>
        public bool puedeEnviarComprobante { get; set; }
        public InstruccionesPagoResponse instrucciones { get; set; } = new();
    }

    public class FacturaLineaResponse
    {
        /// <summary>"PRODUCTO" o "CURSO".</summary>
        public string tipo { get; set; } = "PRODUCTO";
        public int? productoId { get; set; }
        public int? cursoId { get; set; }
        public string nombre { get; set; } = "";
        public int cantidad { get; set; }
        public decimal precio_unitario { get; set; }
        public decimal subtotal { get; set; }
    }

    /// <summary>Un comprobante enviado por el usuario y el resultado de su revisión.</summary>
    public class PagoComprobanteResponse
    {
        public int id { get; set; }
        public int? facturaId { get; set; }
        /// <summary>"SINPE" o "TRANSFERENCIA".</summary>
        public string metodo { get; set; } = "";
        /// <summary>"PENDIENTE", "APROBADO" o "RECHAZADO".</summary>
        public string estado { get; set; } = "";
        public decimal monto { get; set; }
        public string? referencia { get; set; }
        public DateTime fechaEnvio { get; set; }
        public DateTime? fechaRevision { get; set; }
        public string? observacion { get; set; }
        public string? revisadoPor { get; set; }
        public bool esImagen { get; set; }
        public int usuarioId { get; set; }
        public string usuarioNombre { get; set; } = "";
        public string usuarioEmail { get; set; } = "";
    }

    /// <summary>Datos a los que el usuario debe pagar (configurables en la sección "Pagos").</summary>
    public class InstruccionesPagoResponse
    {
        public string sinpeTelefono { get; set; } = "";
        public string sinpeNombre { get; set; } = "";
        public string banco { get; set; } = "";
        public string cuentaIban { get; set; } = "";
        public string cuentaTitular { get; set; } = "";
        public string? cedulaJuridica { get; set; }
    }

    public class RechazarPagoRequest
    {
        public string observacion { get; set; } = "";
    }
}
