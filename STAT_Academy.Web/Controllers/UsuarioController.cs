using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Models.Usuarios;
using STAT_Academy.Web.Services;
using System.Security.Claims;

namespace STAT_Academy.Web.Controllers;

/// <summary>
/// Gestión de usuarios del panel de administración. La API repite todas las validaciones
/// (rol Admin, correo no editable, un Admin no puede desactivarse ni quitarse el rol).
/// </summary>
[Authorize(Roles = "Admin")]
public class UsuarioController : Controller
{
    private static readonly string[] Ordenes = ["nombre", "nombre_desc", "registro", "registro_desc", "login", "login_desc"];

    private readonly ApiUsuarioService _apiUsuarioService;

    public UsuarioController(ApiUsuarioService apiUsuarioService)
    {
        _apiUsuarioService = apiUsuarioService;
    }

    private int UsuarioActualId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public async Task<IActionResult> Index(string? buscar, int? tipo, bool? activo, string? orden, int pagina = 1, int tamano = 10)
    {
        var filtros = new UsuarioFiltros
        {
            Buscar = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim(),
            Tipo = tipo is >= 1 and <= 3 ? tipo : null,
            Activo = activo,
            Orden = Ordenes.Contains(orden) ? orden : null,
            Pagina = Math.Max(1, pagina),
            Tamano = UsuarioFiltros.TamanosPagina.Contains(tamano) ? tamano : 10
        };

        var resultado = await _apiUsuarioService.BuscarAsync(filtros);
        filtros.Pagina = resultado.pagina; // la API ajusta la página si se pasa del final

        return View(new UsuariosIndexViewModel
        {
            Pagina = resultado,
            Filtros = filtros,
            UsuarioActualId = UsuarioActualId
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var usuario = await _apiUsuarioService.GetUsuarioByIdAsync(id);

        if (usuario == null)
            return NotFound();

        ViewBag.EsUsuarioActual = usuario.id == UsuarioActualId;
        return View(usuario);
    }

    [HttpGet]
    public async Task<IActionResult> UserForm(int? id)
    {
        if (id == null)
            return View(new UsuarioFormViewModel());

        var usuario = await _apiUsuarioService.GetUsuarioByIdAsync(id.Value);

        if (usuario == null)
            return NotFound();

        return View(DesdeUsuario(usuario));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserForm(int? id, UsuarioFormViewModel model)
    {
        model.Id = id;

        if (model.EsEdicion)
            return await Editar(model);

        if (string.IsNullOrWhiteSpace(model.Email))
            ModelState.AddModelError(nameof(model.Email), "El correo es obligatorio.");

        if (string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError(nameof(model.Password), "La contraseña es obligatoria.");

        if (!ModelState.IsValid)
            return View(model);

        var (exitoso, mensaje) = await _apiUsuarioService.CrearUsuarioAsync(new CreateUsuarioRequest
        {
            nombre = model.Nombre.Trim(),
            email = model.Email!.Trim(),
            password = model.Password!,
            fk_Tipo_Usuario = model.Tipo
        });

        if (!exitoso)
        {
            ModelState.AddModelError(string.Empty, mensaje);
            model.Password = null;
            return View(model);
        }

        TempData["Success"] = $"Usuario {model.Nombre.Trim()} creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Editar(UsuarioFormViewModel model)
    {
        // El correo y la contraseña no se editan aquí.
        ModelState.Remove(nameof(model.Email));
        ModelState.Remove(nameof(model.Password));

        var actual = await _apiUsuarioService.GetUsuarioByIdAsync(model.Id!.Value);
        if (actual == null)
            return NotFound();

        // Datos de solo lectura para volver a mostrar el formulario.
        var lectura = DesdeUsuario(actual);
        model.Email = lectura.Email;
        model.Activo = lectura.Activo;
        model.FechaRegistro = lectura.FechaRegistro;
        model.UltimoLogin = lectura.UltimoLogin;
        model.EsUsuarioActual = lectura.EsUsuarioActual;

        if (model.EsUsuarioActual)
            model.Tipo = TiposUsuario.Admin; // el propio Admin no puede quitarse el rol

        if (!ModelState.IsValid)
            return View(model);

        var (exitoso, mensaje) = await _apiUsuarioService.ActualizarUsuarioAsync(model.Id.Value, new UpdateUsuarioRequest
        {
            nombre = model.Nombre.Trim(),
            fk_Tipo_Usuario = model.Tipo
        });

        if (!exitoso)
        {
            ModelState.AddModelError(string.Empty, mensaje);
            return View(model);
        }

        TempData["Success"] = "Usuario actualizado correctamente.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ToggleUser(int id, string? volver) => CambiarEstado(id, false, volver);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ActivateUser(int id, string? volver) => CambiarEstado(id, true, volver);

    private async Task<IActionResult> CambiarEstado(int id, bool activo, string? volver)
    {
        var (exitoso, mensaje) = await _apiUsuarioService.CambiarEstadoAsync(id, activo);

        TempData[exitoso ? "Success" : "Error"] = exitoso
            ? (activo ? "Usuario activado correctamente." : "Usuario desactivado correctamente.")
            : mensaje;

        // Vuelve a la misma página de la lista (con sus filtros) o al detalle.
        if (!string.IsNullOrEmpty(volver) && Url.IsLocalUrl(volver))
            return LocalRedirect(volver);

        return RedirectToAction(nameof(Index));
    }

    private UsuarioFormViewModel DesdeUsuario(UsuarioResponse u) => new()
    {
        Id = u.id,
        Nombre = u.nombre,
        Email = u.email,
        Tipo = u.fk_Tipo_Usuario,
        Activo = u.activo,
        FechaRegistro = u.fecha_Registro,
        UltimoLogin = u.ultimo_Login,
        EsUsuarioActual = u.id == UsuarioActualId
    };
}
