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

        /// <summary>Existencias en o por debajo del mínimo.</summary>
        public bool stock_bajo { get; set; }
        public bool agotado { get; set; }

        public string? imagen_principal { get; set; }
        public List<ProductoImagenResponse> imagenes { get; set; } = [];

        /// <summary>Imágenes que no son la principal: van al carrusel del detalle.</summary>
        public IEnumerable<ProductoImagenResponse> ImagenesSecundarias =>
            imagenes.Where(i => i.url != imagen_principal);
    }

    public class ProductoImagenResponse
    {
        public int id { get; set; }
        public string url { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public bool es_principal { get; set; }
    }
}
