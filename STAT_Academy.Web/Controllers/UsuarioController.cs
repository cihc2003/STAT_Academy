using STAT_Academy.Web.Models.Usuarios;
using Microsoft.AspNetCore.Mvc;

public class UsuariosController : Controller
{
    private readonly ApiUsuarioService _ApiUsuarioService;

    public UsuariosController(ApiUsuarioService ApiUsuarioService)
    {
        _ApiUsuarioService = ApiUsuarioService;
    }

    public async Task<IActionResult> Index()
    {
        var usuarios = await _ApiUsuarioService.GetUsuarios();
        return View(usuarios);
    }

    public async Task<IActionResult> Detalles(int id)
    {
        var usuario = await _ApiUsuarioService.GetUsuarioById(id);

        if (usuario == null)
        {
            return NotFound();
        }

        return View(usuario);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(RegisterUsuarioRequest request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var usuario = await _ApiUsuarioService.RegistrarUsuario(request);

        if (usuario == null)
        {
            ModelState.AddModelError("", "No se pudo registrar el usuario.");
            return View(request);
        }

        return RedirectToAction("Index");
    }
}