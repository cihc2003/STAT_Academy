using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Services;
using STAT_Academy.Web.Models.Productos;

namespace STAT_Academy.Web.Controllers;

public class ProductsController : Controller
{
    private readonly ApiProductoService _apiProductos;

    public ProductsController(ApiProductoService apiProductos)
    {
        _apiProductos = apiProductos;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Tienda";
        ViewData["Page"] = "Tienda";

        var productos = await _apiProductos.GetProductos() ?? [];

        // Solo mostramos productos activos y con existencias en la tienda pública.
        var activos = productos
            .Where(p => p.estado)
            .ToList();

        return View(activos);
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