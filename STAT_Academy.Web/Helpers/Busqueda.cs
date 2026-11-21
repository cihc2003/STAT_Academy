using System.Globalization;

namespace STAT_Academy.Web.Helpers
{
    /// <summary>Búsqueda de texto tolerante: sin distinguir mayúsculas ni tildes ("fisica" encuentra "Física").</summary>
    public static class Busqueda
    {
        private static readonly CompareInfo Comparador = CultureInfo.GetCultureInfo("es-CR").CompareInfo;
        private const CompareOptions Opciones = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

        /// <summary>True si no hay término o si el texto lo contiene.</summary>
        public static bool Coincide(string? texto, string? termino) =>
            string.IsNullOrWhiteSpace(termino)
            || (texto != null && Comparador.IndexOf(texto, termino.Trim(), Opciones) >= 0);

        /// <summary>Igualdad sin distinguir mayúsculas ni tildes (para categorías).</summary>
        public static bool Igual(string? a, string? b) =>
            Comparador.Compare(a?.Trim() ?? "", b?.Trim() ?? "", Opciones) == 0;
    }
}
