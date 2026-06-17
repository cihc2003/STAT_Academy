using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Data;
using STAT_Academy.Web.Models;
using STAT_Academy.Web.Models.Api;
using STAT_Academy.Web.Services;
using STAT_Academy.Web.Services.Mappers;
using STAT_Academy.Web.ViewModels;

namespace STAT_Academy.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApiUsuarioService _apiUsuarios;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApiUsuarioService apiUsuarios)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _apiUsuarios = apiUsuarios;
    }

    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        ApiResultado<UsuarioApiResponse> apiResult;
        try
        {
            apiResult = await _apiUsuarios.RegistrarAsync(model);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "No se pudo conectar con la API. Verifica que STAT_Academy.Api esté ejecutándose.");
            return View(model);
        }

        if (!apiResult.Exitoso || apiResult.Datos == null)
        {
            ModelState.AddModelError(string.Empty, apiResult.Mensaje ?? "No se pudo registrar el usuario.");
            return View(model);
        }

        var user = await SincronizarUsuarioLocalAsync(apiResult.Datos.Email, apiResult.Datos.Nombre, apiResult.Datos.Estado, ApiRolMapper.ObtenerRol(apiResult.Datos.TipoUsuario), model.Password);
        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("MisCursos", "Student");
    }

    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        ApiResultado<LoginApiResponse> apiResult;
        try
        {
            apiResult = await _apiUsuarios.IniciarSesionAsync(model);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "No se pudo conectar con la API. Verifica que STAT_Academy.Api esté ejecutándose.");
            return View(model);
        }

        if (!apiResult.Exitoso || apiResult.Datos == null)
        {
            ModelState.AddModelError(string.Empty, apiResult.Mensaje ?? "Correo o contraseña incorrectos.");
            return View(model);
        }

        var user = await SincronizarUsuarioLocalAsync(apiResult.Datos.Email, apiResult.Datos.Nombre, true, ApiRolMapper.ObtenerRol(apiResult.Datos.TipoUsuario));
        await _signInManager.SignInAsync(user, model.RememberMe);
        return LocalRedirect(returnUrl ?? Url.Action("Index", "Home")!);
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        TempData["Success"] = "Si el correo existe, enviaremos instrucciones para recuperar la contraseña.";
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();

    private async Task<ApplicationUser> SincronizarUsuarioLocalAsync(string email, string nombre, bool activo, string rol, string? password = null)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = nombre,
                EmailConfirmed = true,
                IsActive = activo
            };

            var createResult = string.IsNullOrWhiteSpace(password)
                ? await _userManager.CreateAsync(user)
                : await _userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException("No se pudo sincronizar el usuario local.");
            }
        }
        else
        {
            user.FullName = nombre;
            user.IsActive = activo;
            await _userManager.UpdateAsync(user);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        if (!await _userManager.IsInRoleAsync(user, rol))
        {
            await _userManager.AddToRoleAsync(user, rol);
        }

        return user;
    }
}
