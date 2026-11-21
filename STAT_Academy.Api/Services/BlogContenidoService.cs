using Ganss.Xss;
using Microsoft.Extensions.Options;
using STAT_Academy.Api.Configuration;
using System.Net;
using System.Text.RegularExpressions;

namespace STAT_Academy.Api.Services
{
    /// <summary>
    /// Contenido enriquecido del blog (HTML del editor). Todo HTML que entra o sale pasa por
    /// una lista blanca: solo formato de texto, colores e imágenes de nuestro Blob Storage.
    /// Así un autor no puede inyectar scripts que se ejecuten en el navegador de los lectores (XSS).
    /// </summary>
    public class BlogContenidoService
    {
        // Clases que genera el editor (Quill) y las de tamaño/posición de imágenes del blog.
        private static readonly string[] ClasesPermitidas =
        [
            "ql-align-center", "ql-align-right", "ql-align-justify",
            "ql-indent-1", "ql-indent-2", "ql-indent-3", "ql-indent-4",
            "img-sm", "img-md", "img-lg",
            "img-izquierda", "img-centro", "img-derecha"
        ];

        private static readonly Regex Etiquetas = new("<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex Espacios = new(@"\s+", RegexOptions.Compiled);

        private readonly HtmlSanitizer _sanitizer;
        private readonly string _hostImagenes;

        public BlogContenidoService(IOptions<BlobStorageOptions> blob)
        {
            _hostImagenes = new Uri(blob.Value.AccountUrl).Host;

            var s = new HtmlSanitizer();

            s.AllowedTags.Clear();
            foreach (var t in new[] { "p", "br", "strong", "b", "em", "i", "u", "s", "h2", "h3", "h4",
                                      "ul", "ol", "li", "blockquote", "a", "span", "img", "hr", "pre", "code" })
                s.AllowedTags.Add(t);

            s.AllowedAttributes.Clear();
            foreach (var a in new[] { "href", "src", "alt", "class", "style", "data-list", "target", "rel" })
                s.AllowedAttributes.Add(a);

            s.AllowedClasses.Clear();
            foreach (var c in ClasesPermitidas)
                s.AllowedClasses.Add(c);

            // Solo colores: nada de posiciones, tamaños de fuente ni url() en estilos.
            s.AllowedCssProperties.Clear();
            s.AllowedCssProperties.Add("color");
            s.AllowedCssProperties.Add("background-color");

            s.AllowedSchemes.Clear();
            s.AllowedSchemes.Add("https");
            s.AllowedSchemes.Add("http");
            s.AllowedSchemes.Add("mailto");

            s.AllowedAtRules.Clear();

            // Imágenes: solo las de nuestra cuenta de almacenamiento (ni data:, ni sitios externos).
            s.PostProcessNode += (_, e) =>
            {
                if (e.Node is AngleSharp.Html.Dom.IHtmlImageElement img)
                {
                    if (!Uri.TryCreate(img.Source, UriKind.Absolute, out var uri)
                        || uri.Scheme != Uri.UriSchemeHttps
                        || !string.Equals(uri.Host, _hostImagenes, StringComparison.OrdinalIgnoreCase))
                    {
                        img.Remove();
                        return;
                    }

                    img.SetAttribute("loading", "lazy");
                }

                // Los enlaces externos se abren en otra pestaña sin dar acceso a esta página.
                if (e.Node is AngleSharp.Html.Dom.IHtmlAnchorElement a)
                {
                    a.SetAttribute("target", "_blank");
                    a.SetAttribute("rel", "noopener noreferrer nofollow");
                }
            };
            s.AllowedAttributes.Add("loading");

            _sanitizer = s;
        }

        /// <summary>HTML seguro para guardar y mostrar. El texto plano antiguo se convierte a párrafos.</summary>
        public string Limpiar(string? contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return "";

            var html = EsHtml(contenido) ? contenido : TextoAParrafos(contenido);
            return _sanitizer.Sanitize(html).Trim();
        }

        /// <summary>True si, tras limpiar, no queda ni texto ni imágenes.</summary>
        public bool EstaVacio(string htmlLimpio) =>
            string.IsNullOrWhiteSpace(TextoPlano(htmlLimpio))
            && !htmlLimpio.Contains("<img", StringComparison.OrdinalIgnoreCase);

        /// <summary>Texto sin etiquetas, para resúmenes y tiempo de lectura.</summary>
        public static string TextoPlano(string? html)
        {
            if (string.IsNullOrEmpty(html))
                return "";

            var conEspacios = html.Replace("</p>", " </p>").Replace("<br>", " ").Replace("</li>", " </li>");
            return Espacios.Replace(WebUtility.HtmlDecode(Etiquetas.Replace(conEspacios, " ")), " ").Trim();
        }

        public static string Resumen(string? html, int maximo = 220)
        {
            var texto = TextoPlano(html);
            if (texto.Length <= maximo)
                return texto;

            var corte = texto.LastIndexOf(' ', maximo);
            return texto[..(corte > maximo / 2 ? corte : maximo)].TrimEnd(',', '.', ';', ':') + "…";
        }

        /// <summary>Minutos de lectura a ~200 palabras por minuto (mínimo 1).</summary>
        public static int MinutosLectura(string? html)
        {
            var palabras = TextoPlano(html).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            return Math.Max(1, (int)Math.Round(palabras / 200.0));
        }

        private static bool EsHtml(string texto) =>
            Regex.IsMatch(texto, @"<\s*(p|br|strong|em|ul|ol|li|h[2-4]|span|img|a|blockquote)\b", RegexOptions.IgnoreCase);

        private static string TextoAParrafos(string texto) =>
            string.Concat(texto.Replace("\r\n", "\n")
                .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
                .Select(p => $"<p>{WebUtility.HtmlEncode(p.Trim()).Replace("\n", "<br>")}</p>"));
    }
}
