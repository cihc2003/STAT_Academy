using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Web.Data;
using STAT_Academy.Web.Models;
using STAT_Academy.Web.Models.Api;
using STAT_Academy.Web.Services;
using STAT_Academy.Web.Services.Mappers;

namespace STAT_Academy.Web.Controllers;

//[Authorize(Roles = SeedData.AdminRole)]// quitar comentario con el api funcionando
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApiUserGateway _apiGateway;
    private readonly ApiUsuarioService _apiUsuarios;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ApiUserGateway apiGateway, ApiUsuarioService apiUsuarios)
    {
        _db = db;
        _userManager = userManager;
        _apiGateway = apiGateway;
        _apiUsuarios = apiUsuarios;
    }

    public async Task<IActionResult> Dashboard()
    {
        ViewBag.Products = await _db.Products.CountAsync();
        ViewBag.Courses = await _db.Courses.CountAsync();
        ViewBag.Enrollments = await _db.Enrollments.CountAsync();
        ViewBag.Orders = await _db.Orders.CountAsync();
        var apiDisponible = await _apiUsuarios.EstaDisponibleAsync();
        ViewBag.ApiReachable = apiDisponible;
        ViewBag.Users = apiDisponible ? (await _apiUsuarios.ObtenerUsuariosAsync()).Count : await _userManager.Users.CountAsync();
        return View();
    }

    public async Task<IActionResult> Users(string? search, string? role, bool? active)
    {
        List<UsuarioApiResponse> usuariosApi;
        try
        {
            usuariosApi = await _apiUsuarios.ObtenerUsuariosAsync();
        }
        catch
        {
            TempData["Success"] = "No se pudo conectar con la API. Verifica que STAT_Academy.Api este ejecutandose.";
            usuariosApi = [];
        }

        var rows = new List<STAT_Academy.Web.ViewModels.UserAdminViewModel>();

        foreach (var usuario in usuariosApi)
        {
            var localUser = await _userManager.FindByEmailAsync(usuario.Email);
            rows.Add(new STAT_Academy.Web.ViewModels.UserAdminViewModel
            {
                Id = usuario.Id.ToString(),
                FullName = usuario.Nombre,
                Email = usuario.Email,
                PhoneNumber = localUser?.PhoneNumber,
                DocumentId = localUser?.DocumentId ?? string.Empty,
                IsActive = usuario.Estado,
                Role = ApiRolMapper.ObtenerRol(usuario.TipoUsuario)
            });
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            rows = rows.Where(u =>
                u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        if (!string.IsNullOrWhiteSpace(role)) rows = rows.Where(u => u.Role == role).ToList();
        if (active.HasValue) rows = rows.Where(u => u.IsActive == active.Value).ToList();

        ViewBag.Roles = new[] { SeedData.AdminRole, SeedData.TutorRole, SeedData.StudentRole };
        return View(rows);
    }

    public async Task<IActionResult> UserForm(string id)
    {
        if (!int.TryParse(id, out var apiId)) return NotFound();

        var usuario = await _apiUsuarios.ObtenerUsuarioAsync(apiId);
        if (usuario == null) return NotFound();

        var localUser = await _userManager.FindByEmailAsync(usuario.Email);
        ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new[] { SeedData.AdminRole, SeedData.TutorRole, SeedData.StudentRole });
        return View(new STAT_Academy.Web.ViewModels.UserAdminViewModel
        {
            Id = usuario.Id.ToString(),
            FullName = usuario.Nombre,
            Email = usuario.Email,
            PhoneNumber = localUser?.PhoneNumber,
            DocumentId = localUser?.DocumentId ?? string.Empty,
            IsActive = usuario.Estado,
            Role = ApiRolMapper.ObtenerRol(usuario.TipoUsuario)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UserForm(STAT_Academy.Web.ViewModels.UserAdminViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new[] { SeedData.AdminRole, SeedData.TutorRole, SeedData.StudentRole });
            return View(model);
        }

        if (!int.TryParse(model.Id, out var apiId)) return NotFound();
        var usuarioAntes = await _apiUsuarios.ObtenerUsuarioAsync(apiId);
        var apiResult = await _apiUsuarios.ActualizarAsync(apiId, model);
        if (!apiResult.Exitoso || apiResult.Datos == null)
        {
            ModelState.AddModelError(string.Empty, apiResult.Mensaje ?? "No se pudo actualizar el usuario en la API.");
            ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new[] { SeedData.AdminRole, SeedData.TutorRole, SeedData.StudentRole });
            return View(model);
        }

        await SincronizarUsuarioLocalAsync(apiResult.Datos, model.PhoneNumber, model.DocumentId, usuarioAntes?.Email);

        TempData["Success"] = "Usuario actualizado correctamente.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUser(string id)
    {
        if (!int.TryParse(id, out var apiId)) return RedirectToAction(nameof(Users));

        var usuario = await _apiUsuarios.ObtenerUsuarioAsync(apiId);
        if (usuario != null)
        {
            var apiResult = await _apiUsuarios.CambiarEstadoAsync(apiId, !usuario.Estado);
            if (apiResult.Exitoso && apiResult.Datos != null)
            {
                await SincronizarUsuarioLocalAsync(apiResult.Datos);
                TempData["Success"] = apiResult.Datos.Estado ? "Usuario activado." : "Usuario desactivado.";
            }
            else
            {
                TempData["Success"] = apiResult.Mensaje ?? "No se pudo cambiar el estado del usuario.";
            }
        }
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Products(string? search, string? category, bool? active)
    {
        var query = _db.Products.Include(p => p.Supplier).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Name.Contains(search));
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(p => p.Category == category);
        if (active.HasValue) query = query.Where(p => p.IsActive == active);
        ViewBag.Categories = await _db.Products.Select(p => p.Category).Distinct().ToListAsync();
        return View(await query.OrderBy(p => p.Name).ToListAsync());
    }

    public async Task<IActionResult> ProductForm(int? id)
    {
        await LoadSuppliersAsync();
        if (id == null) return View(new Product());
        var item = await _db.Products.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductForm(Product model)
    {
        if (!ModelState.IsValid)
        {
            await LoadSuppliersAsync();
            return View(model);
        }
        if (model.Id == 0) _db.Products.Add(model);
        else _db.Products.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Producto guardado correctamente.";
        return RedirectToAction(nameof(Products));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProduct(int id)
    {
        var item = await _db.Products.FindAsync(id);
        if (item != null)
        {
            item.IsActive = !item.IsActive;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Products));
    }

    public async Task<IActionResult> Suppliers() => View(await _db.Suppliers.OrderBy(s => s.Name).ToListAsync());

    public async Task<IActionResult> SupplierForm(int? id)
    {
        if (id == null) return View(new Supplier());
        var item = await _db.Suppliers.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SupplierForm(Supplier model)
    {
        if (!ModelState.IsValid) return View(model);
        if (model.Id == 0) _db.Suppliers.Add(model);
        else _db.Suppliers.Update(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Suppliers));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSupplier(int id)
    {
        var item = await _db.Suppliers.FindAsync(id);
        if (item != null)
        {
            item.IsActive = !item.IsActive;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Suppliers));
    }

    public async Task<IActionResult> Courses() => View(await _db.Courses.Include(c => c.Tutor).OrderBy(c => c.Title).ToListAsync());

    public async Task<IActionResult> CourseForm(int? id)
    {
        await LoadTutorsAsync();
        if (id == null) return View(new Course { DurationWeeks = 6 });
        var item = await _db.Courses.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CourseForm(Course model)
    {
        if (!ModelState.IsValid)
        {
            await LoadTutorsAsync();
            return View(model);
        }
        if (model.Id == 0) _db.Courses.Add(model);
        else _db.Courses.Update(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Courses));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCourse(int id)
    {
        var item = await _db.Courses.FindAsync(id);
        if (item != null)
        {
            item.IsActive = !item.IsActive;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Courses));
    }

    public async Task<IActionResult> Enrollments() =>
        View(await _db.Enrollments.Include(e => e.Course).Include(e => e.Student).OrderByDescending(e => e.CreatedAt).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEnrollment(int id, string status)
    {
        var enrollment = await _db.Enrollments.FindAsync(id);
        if (enrollment != null && new[] { "Aprobada", "Rechazada", "Pendiente" }.Contains(status))
        {
            enrollment.Status = status;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Enrollments));
    }

    public async Task<IActionResult> Blog() => View(await _db.BlogPosts.OrderByDescending(p => p.CreatedAt).ToListAsync());

    public async Task<IActionResult> BlogForm(int? id)
    {
        if (id == null) return View(new BlogPost());
        var item = await _db.BlogPosts.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BlogForm(BlogPost model)
    {
        if (!ModelState.IsValid) return View(model);
        if (model.Id == 0) _db.BlogPosts.Add(model);
        else _db.BlogPosts.Update(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Blog));
    }

    public async Task<IActionResult> Orders() =>
        View(await _db.Orders.Include(o => o.User).Include(o => o.Items).ThenInclude(i => i.Product).OrderByDescending(o => o.CreatedAt).ToListAsync());

    public async Task<IActionResult> Invoices() =>
        View(await _db.Invoices.Include(i => i.Order).ThenInclude(o => o!.User).OrderByDescending(i => i.IssuedAt).ToListAsync());

    private async Task LoadSuppliersAsync()
    {
        ViewBag.Suppliers = new SelectList(await _db.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(), "Id", "Name");
    }

    private async Task LoadTutorsAsync()
    {
        var tutors = await _userManager.GetUsersInRoleAsync(SeedData.TutorRole);
        ViewBag.Tutors = new SelectList(tutors.Where(t => t.IsActive).OrderBy(t => t.FullName), "Id", "FullName");
    }

    private async Task SincronizarUsuarioLocalAsync(UsuarioApiResponse usuario, string? telefono = null, string? documento = null, string? emailAnterior = null)
    {
        var user = !string.IsNullOrWhiteSpace(emailAnterior)
            ? await _userManager.FindByEmailAsync(emailAnterior)
            : await _userManager.FindByEmailAsync(usuario.Email);

        user ??= await _userManager.FindByEmailAsync(usuario.Email);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = usuario.Email,
                Email = usuario.Email,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(user);
        }

        user.FullName = usuario.Nombre;
        user.Email = usuario.Email;
        user.UserName = usuario.Email;
        user.PhoneNumber = telefono ?? user.PhoneNumber;
        user.DocumentId = documento ?? user.DocumentId;
        user.IsActive = usuario.Estado;
        await _userManager.UpdateAsync(user);

        var role = ApiRolMapper.ObtenerRol(usuario.TipoUsuario);
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Any()) await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!await _userManager.IsInRoleAsync(user, role)) await _userManager.AddToRoleAsync(user, role);
    }
}
