namespace STAT_Academy.Web.Models.Productos
{
    /// <summary>Catálogo de la tienda con sus filtros.</summary>
    public class TiendaViewModel
    {
        public List<ProductoResponse> Productos { get; set; } = [];

        /// <summary>Productos activos antes de filtrar.</summary>
        public int Total { get; set; }

        public string? Buscar { get; set; }
        public string? Categoria { get; set; }

        public List<(string nombre, int cantidad)> Categorias { get; set; } = [];

        public bool Filtrando => !string.IsNullOrWhiteSpace(Buscar) || !string.IsNullOrWhiteSpace(Categoria);
    }
}
