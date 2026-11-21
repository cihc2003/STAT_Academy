namespace STAT_Academy.DTOs.Producto
{
    public class ProductoResponse
    {
        public int id { get; set; }
        public string nombre { get; set; } = "";
        public string categoria { get; set; } = "";
        public string descripcion { get; set; } = "";
        public decimal precio_base { get; set; }
        public int stock { get; set; }
        public int min_stock { get; set; }
        public bool estado { get; set; }
        public int fk_proveedor { get; set; }

        /// <summary>Existencias en o por debajo del mínimo (hay que reponer).</summary>
        public bool stock_bajo { get; set; }
        public bool agotado { get; set; }

        /// <summary>URL pública de la imagen principal, si tiene.</summary>
        public string? imagen_principal { get; set; }

        /// <summary>Todas las imágenes activas, la principal primero.</summary>
        public List<ProductoImagenResponse> imagenes { get; set; } = new();
    }

    public class ProductoImagenResponse
    {
        public int id { get; set; }
        public string url { get; set; } = "";
        public string nombre { get; set; } = "";
        public bool es_principal { get; set; }
    }
}
