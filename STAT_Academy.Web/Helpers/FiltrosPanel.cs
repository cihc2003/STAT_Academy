using Microsoft.AspNetCore.Http;

namespace STAT_Academy.Web.Helpers
{
    /// <summary>
    /// Ayuda para las barras de filtros del panel: arma la ruta actual sin uno o varios filtros
    /// (para los chips "quitar filtro") y el texto de la opción elegida en un selector.
    /// </summary>
    public static class FiltrosPanel
    {
        /// <summary>Parámetros de la consulta actual, sin vacíos y sin las claves indicadas.</summary>
        public static Dictionary<string, string> Sin(IQueryCollection consulta, params string[] claves) =>
            consulta
                .Where(p => !string.IsNullOrWhiteSpace(p.Value.ToString())
                            && !claves.Contains(p.Key, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(p => p.Key, p => p.Value.ToString());

        /// <summary>Valor no vacío de un parámetro de la consulta (o null).</summary>
        public static string? Valor(IQueryCollection consulta, string clave)
        {
            var valor = consulta[clave].ToString();
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        /// <summary>Clase para marcar un selector que tiene un filtro aplicado.</summary>
        public static string Activo(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? "" : "panel-filtros__activo";
    }
}
