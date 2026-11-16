using System.Net;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace STAT_Academy.Web.Helpers
{
    /// <summary>
    /// Aviso en la página (sistema único de notificaciones, variante en línea).
    /// Uso: <c>&lt;aviso tipo="exito|error|advertencia|info" titulo="Opcional"&gt;Texto&lt;/aviso&gt;</c>.
    /// Los errores se anuncian de inmediato (role="alert"); el resto, con cortesía (role="status").
    /// Los mismos colores e íconos usan las notificaciones flotantes (Shared/_Notificaciones).
    /// </summary>
    [HtmlTargetElement("aviso")]
    public class AvisoTagHelper : TagHelper
    {
        public string Tipo { get; set; } = "info";

        public string? Titulo { get; set; }

        /// <summary>False para avisos fijos que no deben anunciarse al cargar (p. ej. instrucciones).</summary>
        public bool Anunciar { get; set; } = true;

        public static string Icono(string tipo) => tipo switch
        {
            "exito" => "exito",
            "error" => "error",
            "advertencia" => "advertencia",
            _ => "info"
        };

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var tipo = Tipo is "exito" or "error" or "advertencia" ? Tipo : "info";
            var contenido = (await output.GetChildContentAsync()).GetContent();

            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;

            var clase = output.Attributes.TryGetAttribute("class", out var extra) ? $" {extra.Value}" : "";
            output.Attributes.SetAttribute("class", $"aviso aviso--{tipo}{clase}");

            if (Anunciar && !output.Attributes.ContainsName("role"))
                output.Attributes.SetAttribute("role", tipo == "error" ? "alert" : "status");

            var titulo = string.IsNullOrWhiteSpace(Titulo)
                ? ""
                : $"<p class=\"aviso__titulo\">{WebUtility.HtmlEncode(Titulo)}</p>";

            output.Content.SetHtmlContent(
                $"<svg class=\"icono aviso__icono\" aria-hidden=\"true\" focusable=\"false\"><use href=\"{Iconos.Sprite}#{Icono(tipo)}\"></use></svg>" +
                $"<div class=\"aviso__cuerpo\">{titulo}<div class=\"aviso__texto\">{contenido}</div></div>");
        }
    }
}
