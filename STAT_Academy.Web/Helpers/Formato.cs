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

        /// <summary>Fecha legible: 5 de enero de 2027</summary>
        public static string FechaLarga(DateTime fecha) => fecha.ToString("d 'de' MMMM 'de' yyyy", CostaRica);

        /// <summary>Iniciales para un respaldo visual sin imagen: "Excel Avanzado" → "EA".</summary>
        public static string Iniciales(string? texto)
        {
            var palabras = (texto ?? "")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(p => p.Length > 2 && char.IsLetterOrDigit(p[0]))
                .Take(2)
                .Select(p => char.ToUpper(p[0], CostaRica))
                .ToArray();

            return palabras.Length > 0 ? new string(palabras) : "C";
        }
    }
}
