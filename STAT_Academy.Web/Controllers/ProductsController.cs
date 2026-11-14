using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Services;
using STAT_Academy.Web.Helpers;
using STAT_Academy.Web.Models.Productos;

namespace STAT_Academy.Web.Controllers;

public class ProductsController : Controller
{
    private readonly ApiProductoService _apiProductos;

    public ProductsController(ApiProductoService apiProductos)
    {
        _apiProductos = apiProductos;
    }

    /// <param name="buscar">Texto a buscar en el nombre (sin distinguir mayúsculas ni tildes).</param>
    /// <param name="categoria">Categoría exacta elegida en el filtro.</param>
    public async Task<IActionResult> Index(string? buscar, string? categoria)
    {
        ViewData["Title"] = "Tienda";
        ViewData["Page"] = "Tienda";

        var productos = await _apiProductos.GetProductos() ?? [];

        // La tienda pública solo muestra productos activos (un Admin los recibe todos de la API).
        var activos = productos.Where(p => p.estado).ToList();

        var filtrados = activos
            .Where(p => Busqueda.Coincide(p.nombre, buscar))
            .Where(p => string.IsNullOrWhiteSpace(categoria) || Busqueda.Igual(p.categoria, categoria))
            // Primero lo que se puede comprar; los agotados al final.
            .OrderBy(p => p.agotado)
            .ThenBy(p => p.nombre)
            .ToList();

        return View(new TiendaViewModel
        {
            Productos = filtrados,
            Total = activos.Count,
            Buscar = buscar?.Trim(),
            Categoria = categoria?.Trim(),
            // Categorías con al menos un producto activo, sin repetir por mayúsculas o tildes.
            Categorias = activos
                .Where(p => !string.IsNullOrWhiteSpace(p.categoria))
                .GroupBy(p => p.categoria.Trim(), StringComparer.Create(new System.Globalization.CultureInfo("es-CR"), System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace))
                .Select(g => (nombre: g.Key, cantidad: g.Count()))
                .OrderBy(c => c.nombre)
                .ToList()
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var producto = await _apiProductos.GetProductoById(id);

        if (producto == null || !producto.estado)
        {
            return NotFound();
        }

        ViewData["Title"] = producto.nombre;
        ViewData["Page"] = "Tienda";

        return View(producto);
    }
}