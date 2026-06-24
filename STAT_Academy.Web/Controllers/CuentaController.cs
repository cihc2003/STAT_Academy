using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Models;
using STAT_Academy.Web.Services;
using STAT_Academy.Web.ViewModels;
using System.Security.Claims;

namespace STAT_Academy.Web.Controllers
{
    public class CuentaController : Controller
    {
        private readonly ApiUserGateway _apiUserGateway;

        public CuentaController(ApiUserGateway apiUserGateway)
        {
            _apiUserGateway = apiUserGateway;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _apiUserGateway.LoginAsync(model);

            if (usuario == null)
            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                return View(model);
            }

            var rol = usuario.tipo switch
            {
                1 => "Admin",
                2 => "Profesor",
                3 => "Estudiante",
                _ => "Usuario"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.id.ToString()),
                new Claim(ClaimTypes.Name, usuario.nombre),
                new Claim(ClaimTypes.Email, usuario.email),
                new Claim(ClaimTypes.Role, rol)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _apiUserGateway.LogoutAsync();

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Account");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}