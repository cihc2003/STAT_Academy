using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Models.Proveedor;
using STAT_Academy.Web.Services;

namespace STAT_Academy.Web.Controllers;

[Authorize(Roles = "Admin")]
public class ProveedorController : Controller
{
    private readonly ApiProveedorService _apiProveedorService;

    public ProveedorController(ApiProveedorService apiProveedorService)
    {
        _apiProveedorService = apiProveedorService;
    }

    public async Task<IActionResult> Index(string? search, bool? active)
    {
        var proveedores = await _apiProveedorService.GetAllAsync() ?? [];

        if (!string.IsNullOrWhiteSpace(search))
        {
            proveedores = proveedores.Where(p =>
                p.nombre.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.contacto.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.email.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (active.HasValue)
        {
            proveedores = proveedores.Where(p => p.estado == active.Value).ToList();
        }

        ViewBag.Search = search;
        ViewBag.Active = active;

        return View(proveedores);
    }

    public async Task<IActionResult> Details(int id)
    {
        var proveedor = await _apiProveedorService.GetByIdAsync(id);

        if (proveedor == null)
        {
            return NotFound();
        }

        return View(proveedor);
    }

    [HttpGet]
    public async Task<IActionResult> SupplierForm(int? id)
    {
        if (id == null)
        {
            return View(new ProveedorCreateRequest());
        }

        var proveedor = await _apiProveedorService.GetByIdAsync(id.Value);

        if (proveedor == null)
        {
            return NotFound();
        }

        return View(new ProveedorCreateRequest
        {
            nombre = proveedor.nombre,
            contacto = proveedor.contacto,
            telefono = proveedor.telefono,
            email = proveedor.email
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SupplierForm(int? id, ProveedorCreateRequest model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (id == null || id == 0)
        {
            var created = await _apiProveedorService.CrearAsync(model);

            if (created == null)
            {
                ModelState.AddModelError(string.Empty, "No se pudo crear el proveedor.");
                return View(model);
            }

            TempData["Success"] = "Proveedor creado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        else
        {
            var updated = await _apiProveedorService.EditarAsync(id.Value, model);

            if (updated == null)
            {
                ModelState.AddModelError(string.Empty, "No se pudo editar el proveedor.");
                return View(model);
            }

            TempData["Success"] = "Proveedor actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSupplier(int id)
    {
        var proveedor = await _apiProveedorService.DesactivarAsync(id);

        if (proveedor == null)
        {
            TempData["Error"] = "No se pudo desactivar el proveedor.";
        }
        else
        {
            TempData["Success"] = "Proveedor desactivado correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }
}