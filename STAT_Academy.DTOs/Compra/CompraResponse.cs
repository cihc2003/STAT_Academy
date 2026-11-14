namespace STAT_Academy.DTOs.Compra
{
    public class CompraResponse
    {
        public int facturaId { get; set; }
        public DateTime fecha { get; set; }
        public decimal total { get; set; }
        public bool pagada { get; set; }
        public List<CompraLineaResponse> lineas { get; set; } = new();
    }

    public class CompraLineaResponse
    {
        public int? productoId { get; set; }
        public string nombre { get; set; } = "";
        public int cantidad { get; set; }
        public decimal precio_unitario { get; set; }
        public decimal subtotal { get; set; }
    }
}
