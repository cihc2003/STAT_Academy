
namespace STAT_Academy.DTOs.Producto
{
    public class ProductoCreateRequest
    {
        public string nombre { get; set; }
        public string categoria { get; set; }
        public string descripcion { get; set; }
        public decimal precio_base { get; set; }
        public int stock { get; set; }
        public int min_stock { get; set; }
        public int fk_proveedor { get; set; }
    }
}