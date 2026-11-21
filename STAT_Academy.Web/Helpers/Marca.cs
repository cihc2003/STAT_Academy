using System.Net;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace STAT_Academy.Web.Helpers
{
    /// <summary>
    /// Referencia única al logotipo de STAT Academy: la estrella roja recortada del logo oficial
    /// de STATMEDICAL (wwwroot/img/marca). Para cambiar el logo basta con reemplazar estos archivos
    /// o estas rutas; las vistas usan <c>@Html.Logo()</c> y el layout <see cref="Iconos"/>.
    /// </summary>
    public static class Marca
    {
        /// <summary>Estrella en alta resolución (343 × 343, fondo transparente), para pantallas HiDPI.</summary>
        public const string LogoEstrella = "/img/marca/statmedical-estrella.png";

        /// <summary>Logo oficial completo (horizontal), por si se necesita en impresos o documentos.</summary>
        public const string LogoCompleto = "/img/marca/statmedical-logo.png";

        public const string Favicon = "/favicon.ico";
        public const string Favicon32 = "/img/marca/favicon-32.png";
        public const string Icono192 = "/img/marca/icono-192.png";
        public const string IconoApple = "/img/marca/apple-touch-icon.png";

        /// <summary>
        /// Logotipo (cuadrado, sin deformar). Decorativo por defecto: el enlace o el texto que lo
        /// acompaña ya nombra la marca; pasar <paramref name="alt"/> si aparece solo.
        /// </summary>
        public static IHtmlContent Logo(this IHtmlHelper _, string? clase = null, int tamano = 36, string alt = "")
        {
            var clases = string.IsNullOrWhiteSpace(clase) ? "logo" : $"logo {clase}";
            return new HtmlString(
                $"<img class=\"{WebUtility.HtmlEncode(clases)}\" src=\"{LogoEstrella}\" width=\"{tamano}\" height=\"{tamano}\" " +
                $"alt=\"{WebUtility.HtmlEncode(alt)}\" decoding=\"async\" />");
        }
    }
}
