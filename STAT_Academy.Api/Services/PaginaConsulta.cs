namespace STAT_Academy.Api.Services
{
    /// <summary>Ajusta página y tamaño pedidos a rangos válidos para paginar en la base de datos.</summary>
    public static class PaginaConsulta
    {
        /// <summary>
        /// Tamaño entre 1 y <paramref name="tamanoMaximo"/>; página entre 1 y la última
        /// (con 0 resultados hay una sola página vacía).
        /// </summary>
        public static (int pagina, int tamano, int totalPaginas) Normalizar(int pagina, int tamano, int total, int tamanoMaximo)
        {
            tamano = Math.Clamp(tamano, 1, tamanoMaximo);
            var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamano));
            return (Math.Clamp(pagina, 1, totalPaginas), tamano, totalPaginas);
        }
    }
}
