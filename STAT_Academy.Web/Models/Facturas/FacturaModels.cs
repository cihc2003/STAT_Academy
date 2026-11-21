using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Facturas
{
    // Contratos de api/Factura (mismos nombres que STAT_Academy.DTOs.Factura).

    public class FacturaResumen
    {
        public int id { get; set; }
        public DateTime fecha { get; set; }
        public decimal total { get; set; }
        public bool pagada { get; set; }
        public string estado { get; set; } = "";
        public string descripcion { get; set; } = "";
        public int usuarioId { get; set; }
        public string usuarioNombre { get; set; } = "";
        public string usuarioEmail { get; set; } = "";
        public int cantidadComprobantes { get; set; }
    }

    public class FacturaDetalle : FacturaResumen
    {
        public List<FacturaLinea> lineas { get; set; } = [];
        public List<PagoComprobante> pagos { get; set; } = [];
        public bool puedeEnviarComprobante { get; set; }
        public InstruccionesPago instrucciones { get; set; } = new();
    }

    public class FacturaLinea
    {
        public string tipo { get; set; } = "PRODUCTO";
        public int? productoId { get; set; }
        public int? cursoId { get; set; }
        public string nombre { get; set; } = "";
        public int cantidad { get; set; }
        public decimal precio_unitario { get; set; }
        public decimal subtotal { get; set; }
    }

    public class PagoComprobante
    {
        public int id { get; set; }
        public int? facturaId { get; set; }
        public string metodo { get; set; } = "";
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

    public class InstruccionesPago
    {
        public string sinpeTelefono { get; set; } = "";
        public string sinpeNombre { get; set; } = "";
        public string banco { get; set; } = "";
        public string cuentaIban { get; set; } = "";
        public string cuentaTitular { get; set; } = "";
        public string? cedulaJuridica { get; set; }
    }

    /// <summary>Formulario para subir el comprobante de pago.</summary>
    public class EnviarComprobanteViewModel
    {
        /// <summary>Un archivo de hasta 10 MB + margen del formulario.</summary>
        public const long MaxBytesPeticion = 11 * 1024 * 1024;
        public const string Aceptados = ".jpg,.jpeg,.png,.webp,.pdf";

        public int FacturaId { get; set; }

        [Required(ErrorMessage = "Selecciona cómo pagaste.")]
        public string Metodo { get; set; } = "SINPE";

        [StringLength(100, ErrorMessage = "La referencia admite hasta 100 caracteres.")]
        public string? Referencia { get; set; }

        [Required(ErrorMessage = "Adjunta el comprobante de pago.")]
        public IFormFile? Archivo { get; set; }
    }

    /// <summary>Panel de administración: facturas y comprobantes.</summary>
    public class FacturasAdminViewModel
    {
        /// <summary>"pagos" (comprobantes) o "facturas".</summary>
        public string Vista { get; set; } = "pagos";
        public string? Estado { get; set; }
        public string? Buscar { get; set; }
        public int Pendientes { get; set; }
        public List<PagoComprobante> Pagos { get; set; } = [];
        public List<FacturaResumen> Facturas { get; set; } = [];
    }
}
