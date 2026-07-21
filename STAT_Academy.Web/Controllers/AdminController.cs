using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Services;
using STAT_Academy.Web.Models.Admin;
using STAT_Academy.Web.Models.Productos;

namespace STAT_Academy.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApiUsuarioService _apiUsuarios;
    private readonly ApiProductoService _apiProductos;
    private readonly ApiProveedorService _apiProveedores;

    public AdminController(ApiUsuarioService apiUsuarios, ApiProductoService apiProductos,
        ApiProveedorService apiProveedores)
    {
        _apiUsuarios = apiUsuarios;
        _apiProductos = apiProductos;
        _apiProveedores = apiProveedores;
    }

    public async Task<IActionResult> Dashboard()
    {
        var usuarios = await _apiUsuarios.GetUsuarios();

        ViewBag.Users = usuarios?.Count ?? 0;

        // Temporales mientras no existen estas APIs.
        ViewBag.Products = 0;
        ViewBag.Courses = 0;
        ViewBag.Enrollments = 0;
        ViewBag.Orders = 0;
        ViewBag.ApiReachable = usuarios != null;

        return View();
    }

    public async Task<IActionResult> Usuarios(string? search, string? role, bool? active)
    {
        var usuariosApi = await _apiUsuarios.GetUsuarios() ?? [];

        var rows = usuariosApi.Select(usuario => new UsuarioViewModel
        {
            id = usuario.id,
            nombre = usuario.nombre,
            email = usuario.email,
            fk_Tipo_Usuario = usuario.fk_Tipo_Usuario
        }).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            rows = rows.Where(u =>
                u.nombre.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                u.email.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return View(rows);
    }

    public async Task<IActionResult> UsuarioForm(string id)
    {
        if (!int.TryParse(id, out var apiId))
        {
            return NotFound();
        }

        var usuario = await _apiUsuarios.GetUsuarioById(apiId);

        if (usuario == null)
        {
            return NotFound();
        }

        var model = new UsuarioViewModel
        {
            id = usuario.id,
            nombre = usuario.nombre,
            email = usuario.email,
            fk_Tipo_Usuario = usuario.fk_Tipo_Usuario
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUser(string id)
    {
        if (!int.TryParse(id, out var apiId))
        {
            return RedirectToAction(nameof(Usuarios));
        }

        var usuario = await _apiUsuarios.DesactivarUsuario(apiId);

        if (usuario == null)
        {
            TempData["Error"] = "No se pudo desactivar el usuario.";
        }
        else
        {
            TempData["Success"] = "Usuario desactivado correctamente.";
        }

        return RedirectToAction(nameof(Usuarios));
    }

    public async Task<IActionResult> Products(string? search)
    {
        var productosApi = await _apiProductos.GetProductos() ?? [];

        var rows = productosApi.Select(p => new ProductoViewModel
        {
            id = p.id,
            nombre = p.nombre,
            categoria = p.categoria,
            descripcion = p.descripcion,
            precio_base = p.precio_base,
            stock = p.stock,
            min_stock = p.min_stock,
            estado = p.estado,
            fk_proveedor = p.fk_proveedor
        }).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            rows = rows.Where(p =>
                p.nombre.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.categoria.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return View(rows);
    }

    // GET: formulario vacío (crear) cuando id es null, o precargado (editar) cuando trae id.
    public async Task<IActionResult> ProductForm(int? id)
    {
        var proveedores = await _apiProveedores.GetProveedores() ?? [];

        if (id == null)
        {
            return View(new ProductoFormViewModel
            {
                ProveedoresDisponibles = proveedores
            });
        }

        var producto = await _apiProductos.GetProductoById(id.Value);

        if (producto == null)
        {
            return NotFound();
        }

        var model = new ProductoFormViewModel
        {
            id = producto.id,
            nombre = producto.nombre,
            categoria = producto.categoria,
            descripcion = producto.descripcion,
            precio_base = producto.precio_base,
            stock = producto.stock,
            min_stock = producto.min_stock,
            fk_proveedor = producto.fk_proveedor,
            ProveedoresDisponibles = proveedores
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductForm(ProductoFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.ProveedoresDisponibles = await _apiProveedores.GetProveedores() ?? [];
            return View(model);
        }

        var request = new ProductoCreateRequest
        {
            nombre = model.nombre,
            categoria = model.categoria,
            descripcion = model.descripcion,
            precio_base = model.precio_base,
            stock = model.stock,
            min_stock = model.min_stock,
            fk_proveedor = model.fk_proveedor
        };

        var resultado = model.id == null
            ? await _apiProductos.CrearProducto(request)
            : await _apiProductos.EditarProducto(model.id.Value, request);

        if (resultado == null)
        {
            ModelState.AddModelError("", "No se pudo guardar el producto. Verifica los datos e intenta de nuevo.");
            model.ProveedoresDisponibles = await _apiProveedores.GetProveedores() ?? [];
            return View(model);
        }

        TempData["Success"] = model.id == null
            ? "Producto creado correctamente."
            : "Producto actualizado correctamente.";

        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProduct(int id)
    {
        var producto = await _apiProductos.DesactivarProducto(id);

        if (producto == null)
        {
            TempData["Error"] = "No se pudo eliminar el producto.";
        }
        else
        {
            TempData["Success"] = "Producto eliminado de la tienda correctamente.";
        }

        return RedirectToAction(nameof(Products));
    }
}