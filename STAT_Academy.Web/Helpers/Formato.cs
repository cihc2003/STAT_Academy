using System.Globalization;

namespace STAT_Academy.Web.Helpers
{
    /// <summary>
    /// Formatos de presentación independientes de la configuración regional del servidor
    /// (en Azure el servidor suele estar en inglés y "C" mostraría dólares).
    /// </summary>
    public static class Formato
    {
        private static readonly CultureInfo CostaRica = CultureInfo.GetCultureInfo("es-CR");

        /// <summary>Precio en colones: ₡45 000,00</summary>
        public static string Colones(decimal monto) => monto.ToString("C", CostaRica);
    }
}
