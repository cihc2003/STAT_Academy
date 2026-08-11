using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Models.Contrasena;
using STAT_Academy.Web.Models.Cuenta;
using STAT_Academy.Web.Models.Usuarios;
using STAT_Academy.Web.Services;
using System.Security.Claims;
using STAT_Academy.Web.Models.Correo;

namespace STAT_Academy.Web.Controllers
{
    public class CuentaController : Controller
    {
        private readonly ApiUserGateway _apiUserGateway;
        private readonly ApiUsuarioService _apiUsuarioService;
        private readonly ApiContrasenaService _apiContrasenaService;
        private readonly ApiCorreoService _apiCorreoService;

        public CuentaController(
    ApiUserGateway apiUserGateway,
    ApiUsuarioService apiUsuarioService,
    ApiContrasenaService apiContrasenaService,
    ApiCorreoService apiCorreoService)
        {
            _apiUserGateway = apiUserGateway;
            _apiUsuarioService = apiUsuarioService;
            _apiContrasenaService = apiContrasenaService;
            _apiCorreoService = apiCorreoService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _apiUserGateway.LoginAsync(model);

            if (usuario == null)
            {
                ModelState.AddModelError(
                    "",
                    "Usuario o contraseña incorrectos."
                );

                return View(model);
            }

            var rol = usuario.fk_Tipo_Usuario switch
            {
                1 => "Admin",
                2 => "Tutor",
                3 => "Cliente",
                _ => "Usuario"
            };

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    usuario.id.ToString()
                ),
                new Claim(ClaimTypes.Name, usuario.nombre),
                new Claim(ClaimTypes.Email, usuario.email),
                new Claim(ClaimTypes.Role, rol)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties
            );

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _apiUserGateway.LogoutAsync();

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction(nameof(Login));
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Registrar()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(
            RegisterUsuarioRequest model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var resultado = await _apiUsuarioService.RegistrarUsuarioAsync(
     new RegisterUsuarioRequest
     {
         nombre = model.nombre,
         email = model.email,
         password = model.password,
     }
 );

            if (resultado == null)
            {
                ModelState.AddModelError(
                    "",
                    "No se pudo registrar el usuario."
                );

                return View(model);
            }

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Forgotpassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Forgotpassword(
            SolicitarRecuperacionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var resultado = await _apiContrasenaService
                .SolicitarRecuperacion(model);

            if (!resultado)
            {
                ModelState.AddModelError(
                    "",
                    "No se pudo procesar la solicitud. Intente nuevamente."
                );

                return View(model);
            }

            ViewBag.Mensaje =
                "Si el correo está registrado, recibirá un enlace " +
                "para restablecer la contraseña.";

            ModelState.Clear();

            return View(new SolicitarRecuperacionViewModel());
        }

        [HttpGet]
        public IActionResult RestablecerContrasena(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction(nameof(Login));
            }

            var model = new RestablecerContrasenaViewModel
            {
                token = token
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerContrasena(
            RestablecerContrasenaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var resultado = await _apiContrasenaService
                .RestablecerContrasena(model);

            if (!resultado)
            {
                ModelState.AddModelError(
                    "",
                    "El enlace es inválido, expiró o ya fue utilizado."
                );

                return View(model);
            }

            TempData["Success"] =
                "Contraseña restablecida correctamente. " +
                "Ya puede iniciar sesión.";

            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        [HttpGet]
        public IActionResult CambiarContrasena()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContrasena(
            CambiarContrasenaViewModel model)
        {
            var usuarioIdTexto = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (!int.TryParse(usuarioIdTexto, out var usuarioId))
            {
                return RedirectToAction(nameof(Login));
            }

            model.usuarioId = usuarioId;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var resultado = await _apiContrasenaService
                .CambiarContrasena(model);

            if (!resultado)
            {
                ModelState.AddModelError(
                    "",
                    "No se pudo cambiar la contraseña. " +
                    "Verifique la contraseña actual."
                );

                return View(model);
            }

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            TempData["Success"] =
                "Contraseña cambiada correctamente. " +
                "Inicie sesión nuevamente.";

            return RedirectToAction(nameof(Login));
        }
        [Authorize]
        [HttpGet]
        public IActionResult CambiarCorreo()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarCorreo(
            SolicitarCambioCorreoViewModel model)
        {
            var usuarioIdTexto = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (!int.TryParse(usuarioIdTexto, out var usuarioId))
            {
                return RedirectToAction(nameof(Login));
            }

            model.usuarioId = usuarioId;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var resultado = await _apiCorreoService
                .SolicitarCambio(model);

            if (!resultado.exitoso)
            {
                ModelState.AddModelError(
                    "",
                    resultado.mensaje
                );

                return View(model);
            }

            ViewBag.Mensaje = resultado.mensaje;

            ModelState.Clear();

            return View(new SolicitarCambioCorreoViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmarCambioCorreo(
            string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] =
                    "El enlace de confirmación no es válido.";

                return RedirectToAction(nameof(Login));
            }

            var model = new ConfirmarCambioCorreoViewModel
            {
                token = token
            };

            var resultado = await _apiCorreoService
                .ConfirmarCambio(model);

            if (!resultado.exitoso)
            {
                TempData["Error"] = resultado.mensaje;

                return RedirectToAction(nameof(Login));
            }

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            TempData["Success"] =
                "El correo fue cambiado correctamente. " +
                "Inicie sesión con el correo nuevo.";

            return RedirectToAction(nameof(Login));
        }
    }
}