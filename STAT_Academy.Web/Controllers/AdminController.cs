using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Services;

public class AdminController : Controller
{
    private readonly ApiUsuarioService _apiUsuarioService;

    public AdminController(ApiUsuarioService apiUsuarioService)
    {
        _apiUsuarioService = apiUsuarioService;
    }

    public async Task<IActionResult> Dashboard()
    {
        var usuarios = await _apiUsuarioService.GetUsuariosAsync();

        ViewBag.Users = usuarios?.Count ?? 0;
        ViewBag.Products = 0;
        ViewBag.Courses = 0;
        ViewBag.Enrollments = 0;
        ViewBag.Orders = 0;

        return View();
    }

    public async Task<IActionResult> Users()
    {
        var usuarios = await _apiUsuarioService.GetUsuariosAsync() ?? [];
        return View(usuarios);
    }
}