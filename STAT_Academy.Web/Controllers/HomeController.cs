using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Models;
using STAT_Academy.Web.Models.Blog;
using STAT_Academy.Web.Models.Home;
using STAT_Academy.Web.Models.Productos;
using STAT_Academy.Web.Services;

namespace STAT_Academy.Web.Controllers
{
    public class HomeController : Controller
    {
        private static readonly JsonSerializerOptions OpcionesJson = new() { PropertyNameCaseInsensitive = true };

        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _http;
        private readonly ApiProductoService _productos;
        private readonly ApiBlogService _blog;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory,
            ApiProductoService productos, ApiBlogService blog)
        {
            _logger = logger;
            _http = httpClientFactory.CreateClient(ApiClient.Nombre);
            _productos = productos;
            _blog = blog;
        }

        /// <summary>Portada: solo lee los catálogos públicos que ya existen en la API.</summary>
        public async Task<IActionResult> Index()
        {
            var modelo = new InicioViewModel();

            var cursos = CargarCursos();
            var productos = Intentar("productos", async () => await _productos.GetProductos() ?? []);
            var articulos = Intentar("blog", () => _blog.ObtenerBlogs());
            await Task.WhenAll(cursos, productos, articulos);

            if (cursos.Result is { } listaCursos)
            {
                modelo.CursosDisponibles = true;
                // Mismo criterio que el catálogo público: activos y con inscripción abierta.
                modelo.Cursos = listaCursos
                    .Where(c => c.Estado && c.InscripcionAbierta)
                    .OrderBy(c => c.Fecha_Inicio)
                    .ThenBy(c => c.Nombre)
                    .ToList();
                modelo.Instructores = modelo.Cursos
                    .Where(c => c.FK_Tutor > 0)
                    .Select(c => c.FK_Tutor)
                    .Distinct()
                    .Count();
            }

            if (productos.Result is { } listaProductos)
            {
                modelo.ProductosDisponibles = true;
                var activos = listaProductos.Where(p => p.estado).ToList();
                modelo.Productos = activos.OrderBy(p => p.agotado).ThenBy(p => p.nombre).ToList();
                modelo.Categorias = activos
                    .Where(p => !string.IsNullOrWhiteSpace(p.categoria))
                    .GroupBy(p => p.categoria.Trim(), StringComparer.Create(new CultureInfo("es-CR"),
                        CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace))
                    .Select(g => (nombre: g.Key, cantidad: g.Count()))
                    .OrderByDescending(c => c.cantidad)
                    .ThenBy(c => c.nombre)
                    .ToList();
            }

            if (articulos.Result is { } listaArticulos)
            {
                modelo.BlogDisponible = true;
                modelo.Articulos = listaArticulos
                    .Where(a => a.Estado)
                    .OrderByDescending(a => a.FechaCreacion)
                    .ToList();
            }

            return View(modelo);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Contacto()
        {
            return View();
        }

        private async Task<List<CursoModel>?> CargarCursos()
        {
            try
            {
                using var response = await _http.GetAsync("api/Curso");
                if (!response.IsSuccessStatusCode)
                    return null;

                await using var contenido = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<List<CursoModel>>(contenido, OpcionesJson) ?? [];
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "La portada no pudo cargar los cursos.");
                return null;
            }
        }

        /// <summary>La portada sigue funcionando aunque una fuente falle: esa sección simplemente no se muestra.</summary>
        private async Task<T?> Intentar<T>(string fuente, Func<Task<T>> cargar) where T : class
        {
            try
            {
                return await cargar();
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or NotSupportedException)
            {
                _logger.LogWarning(ex, "La portada no pudo cargar {Fuente}.", fuente);
                return null;
            }
        }
    }
}
