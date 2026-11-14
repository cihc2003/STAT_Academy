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
