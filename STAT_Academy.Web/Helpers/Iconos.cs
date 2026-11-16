using System.Net;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace STAT_Academy.Web.Helpers
{
    /// <summary>
    /// Íconos del sitio: una sola familia en <c>wwwroot/img/iconos.svg</c> (símbolos de 24 × 24).
    /// Grosor, color y tamaño salen de la clase <c>.icono</c> de site.css:
    /// <c>.icono--sm</c> (16 px), por defecto 20 px, <c>.icono--lg</c> (24 px), <c>.icono--xl</c> (32 px).
    /// </summary>
    public static class Iconos
    {
        /// <summary>Cambiar al editar iconos.svg para que el navegador no use la copia vieja.</summary>
        public const string Sprite = "/img/iconos.svg?v=1";

        /// <summary>
        /// Ícono decorativo (oculto para lectores de pantalla). Si el ícono es lo único que
        /// comunica algo, pasar <paramref name="etiqueta"/> para que se anuncie.
        /// </summary>
        public static IHtmlContent Icono(this IHtmlHelper _, string nombre, string? clase = null, string? etiqueta = null)
        {
            var clases = string.IsNullOrWhiteSpace(clase) ? "icono" : $"icono {clase}";
            var accesible = string.IsNullOrWhiteSpace(etiqueta)
                ? "aria-hidden=\"true\""
                : $"role=\"img\" aria-label=\"{WebUtility.HtmlEncode(etiqueta)}\"";

            return new HtmlString(
                $"<svg class=\"{WebUtility.HtmlEncode(clases)}\" {accesible} focusable=\"false\">" +
                $"<use href=\"{Sprite}#{WebUtility.HtmlEncode(nombre)}\"></use></svg>");
        }
    }
}
