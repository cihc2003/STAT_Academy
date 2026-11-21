namespace STAT_Academy.Web.Models.Carrito
{
    /// <summary>Comprobante de compra devuelto por api/Compra.</summary>
    public class CompraResponse
    {
        public int facturaId { get; set; }
        public DateTime fecha { get; set; }
        public decimal total { get; set; }
        public bool pagada { get; set; }
        public List<CompraLineaResponse> lineas { get; set; } = [];
    }

    public class CompraLineaResponse
    {
        public int? productoId { get; set; }
        public string nombre { get; set; } = string.Empty;
        public int cantidad { get; set; }
        public decimal precio_unitario { get; set; }
        public decimal subtotal { get; set; }
    }
}
