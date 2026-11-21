namespace STAT_Academy.Web.Models.Cuenta
{
    /// <summary>
    /// Versión vigente de los términos y condiciones y de la política de cookies.
    /// Cámbiela (y la fecha visible) cada vez que se modifique el texto de alguna de las dos páginas:
    /// la API guarda con cada registro la versión que la persona aceptó.
    /// </summary>
    public static class TerminosVigentes
    {
        public const string Version = "2026-11-16";

        /// <summary>Fecha de la última actualización, tal como se muestra en las páginas.</summary>
        public const string FechaVisible = "16 de noviembre de 2026";
    }
}
