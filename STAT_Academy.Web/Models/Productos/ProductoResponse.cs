namespace STAT_Academy.Web.Models.Productos
{
    public class ProductoResponse
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string categoria { get; set; } = string.Empty;
        public string descripcion { get; set; } = string.Empty;

        public decimal precio_base { get; set; }
        public int stock { get; set; }
        public int min_stock { get; set; }

        public bool estado { get; set; }
        public int fk_proveedor { get; set; }
    }
}
