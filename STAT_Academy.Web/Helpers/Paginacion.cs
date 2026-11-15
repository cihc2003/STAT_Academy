namespace STAT_Academy.Web.Helpers
{
    /// <summary>
    /// Paginación en la Web de las listas del panel que la API devuelve completas
    /// (cursos, productos, proveedores, facturas y comprobantes). La vista la recibe en
    /// ViewBag.Paginacion y la dibuja con el parcial _PanelPaginacion.
    /// </summary>
    public class Paginacion
    {
        public static readonly int[] TamanosPagina = [10, 25, 50];

        public int Pagina { get; private init; }
        public int Tamano { get; private init; }
        public int Total { get; private init; }
        public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)Tamano));
        public int Desde => Total == 0 ? 0 : (Pagina - 1) * Tamano + 1;
        public int Hasta => Math.Min(Pagina * Tamano, Total);

        /// <summary>Texto para el resumen ("cursos", "productos"…).</summary>
        public string Elementos { get; init; } = "registros";

        /// <summary>Corta la lista a la página pedida (fuera de rango: la más cercana).</summary>
        public static (List<T> items, Paginacion paginacion) Aplicar<T>(
            IEnumerable<T> lista, int pagina, int tamano, string elementos)
        {
            var todos = lista as IList<T> ?? lista.ToList();
            tamano = TamanosPagina.Contains(tamano) ? tamano : TamanosPagina[0];

            var info = new Paginacion { Tamano = tamano, Total = todos.Count, Elementos = elementos };
            info = new Paginacion
            {
                Tamano = tamano,
                Total = todos.Count,
                Elementos = elementos,
                Pagina = Math.Clamp(pagina, 1, info.TotalPaginas)
            };

            return (todos.Skip((info.Pagina - 1) * tamano).Take(tamano).ToList(), info);
        }

        /// <summary>Páginas visibles: primera, última y dos alrededor de la actual.</summary>
        public List<int> PaginasVisibles() =>
            Enumerable.Range(1, TotalPaginas)
                .Where(n => n == 1 || n == TotalPaginas || Math.Abs(n - Pagina) <= 2)
                .ToList();
    }
}
