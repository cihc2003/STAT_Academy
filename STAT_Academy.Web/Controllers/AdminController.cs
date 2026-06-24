using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Services;
using STAT_Academy.Web.Models.Admin;

namespace STAT_Academy.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApiUsuarioService _apiUsuarios;

    public AdminController(ApiUsuarioService apiUsuarios)
    {
        _apiUsuarios = apiUsuarios;
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
}