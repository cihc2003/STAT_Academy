namespace STAT_Academy.Web.Models.Carrito
{
    public class CarritoItemResponse
    {
        public int detalleId { get; set; }
        public int productoId { get; set; }
        public string nombre { get; set; } = string.Empty;
        public decimal precio_unitario { get; set; }
        public int cantidad { get; set; }
        public int stock_disponible { get; set; }
        public decimal subtotal { get; set; }
    }

    public class CarritoResponse
    {
        public int carritoId { get; set; }
        public List<CarritoItemResponse> items { get; set; } = [];
        public decimal total { get; set; }
    }
}