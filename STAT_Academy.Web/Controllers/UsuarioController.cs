using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using STAT_Academy.Web.Models.Usuarios;
using STAT_Academy.Web.Services;

namespace STAT_Academy.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UsuarioController : Controller
{
    private readonly ApiUsuarioService _apiUsuarioService;

    public UsuarioController(ApiUsuarioService apiUsuarioService)
    {
        _apiUsuarioService = apiUsuarioService;
    }

    public async Task<IActionResult> Index(string? search, int? tipo, bool? active)
    {
        var usuarios = await _apiUsuarioService.GetUsuariosAsync() ?? [];

        if (!string.IsNullOrWhiteSpace(search))
        {
            usuarios = usuarios.Where(u =>
                u.nombre.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                u.email.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (tipo.HasValue)
        {
            usuarios = usuarios.Where(u => u.fk_Tipo_Usuario == tipo.Value).ToList();
        }

        if (active.HasValue)
        {
            usuarios = usuarios.Where(u => u.activo == active.Value).ToList();
        }

        ViewBag.Search = search;
        ViewBag.Tipo = tipo;
        ViewBag.Active = active;
        ViewBag.Tipos = new SelectList(new[]
        {
            new { Value = 1, Text = "Admin" },
            new { Value = 2, Text = "Tutor" },
            new { Value = 3, Text = "Cliente" }
        }, "Value", "Text", tipo);

        return View(usuarios);
    }

    public async Task<IActionResult> Details(int id)
    {
        var usuario = await _apiUsuarioService.GetUsuarioByIdAsync(id);

        if (usuario == null)
        {
            return NotFound();
        }

        return View(usuario);
    }

    [HttpGet]
    public async Task<IActionResult> UserForm(int? id)
    {
        ViewBag.Tipos = new SelectList(new[]
        {
            new { Value = 1, Text = "Admin" },
            new { Value = 2, Text = "Tutor" },
            new { Value = 3, Text = "Cliente" }
        }, "Value", "Text");

        if (id == null)
        {
            return View(new UpdateUsuarioRequest());
        }

        var usuario = await _apiUsuarioService.GetUsuarioByIdAsync(id.Value);

        if (usuario == null)
        {
            return NotFound();
        }

        return View(new UpdateUsuarioRequest
        {
            nombre = usuario.nombre,
            email = usuario.email,
            fk_Tipo_Usuario = usuario.fk_Tipo_Usuario
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserForm(int? id, UpdateUsuarioRequest model)
    {
        ViewBag.Tipos = new SelectList(new[]
        {
            new { Value = 1, Text = "Admin" },
            new { Value = 2, Text = "Tutor" },
            new { Value = 3, Text = "Cliente" }
        }, "Value", "Text", model.fk_Tipo_Usuario);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (id == null || id == 0)
        {
            var creado = await _apiUsuarioService.RegistrarUsuarioAsync(new RegisterUsuarioRequest
            {
                nombre = model.nombre,
                email = model.email,
                password = model.password,
                fk_Tipo_Usuario = model.fk_Tipo_Usuario
            });

            if (creado == null)
            {
                ModelState.AddModelError(string.Empty, "No se pudo registrar el usuario.");
                return View(model);
            }

            TempData["Success"] = "Usuario registrado correctamente.";
        }
        else
        {
            var actualizado = await _apiUsuarioService.ActualizarUsuarioAsync(id.Value, model);

            if (actualizado == null)
            {
                ModelState.AddModelError(string.Empty, "No se pudo actualizar el usuario.");
                return View(model);
            }

            TempData["Success"] = "Usuario actualizado correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUser(int id)
    {
        var usuario = await _apiUsuarioService.DesactivarUsuarioAsync(id);

        if (usuario == null)
        {
            TempData["Error"] = "No se pudo desactivar el usuario.";
        }
        else
        {
            TempData["Success"] = "Usuario desactivado correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }
}