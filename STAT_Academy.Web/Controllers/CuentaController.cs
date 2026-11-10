using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Models.Contrasena;
using STAT_Academy.Web.Models.Cuenta;
using STAT_Academy.Web.Models.Usuarios;
using STAT_Academy.Web.Services;
using System.Security.Claims;

namespace STAT_Academy.Web.Controllers
{
    public class CuentaController : Controller
    {
        private readonly ApiUserGateway _apiUserGateway;
        private readonly ApiUsuarioService _apiUsuarioService;
        private readonly ApiContrasenaService _apiContrasenaService;

        public CuentaController(
    ApiUserGateway apiUserGateway,
    ApiUsuarioService apiUsuarioService,
    ApiContrasenaService apiContrasenaService)
        {
            _apiUserGateway = apiUserGateway;
            _apiUsuarioService = apiUsuarioService;
            _apiContrasenaService = apiContrasenaService;
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

            var expiraToken = new DateTimeOffset(
                DateTime.SpecifyKind(usuario.expiraEn, DateTimeKind.Utc));

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false,
                // La sesión nunca dura más que el token de la API.
                ExpiresUtc = expiraToken < DateTimeOffset.UtcNow.AddMinutes(30)
                    ? expiraToken
                    : DateTimeOffset.UtcNow.AddMinutes(30)
            };

            // El JWT viaja cifrado dentro de la cookie y BearerTokenHandler lo reenvía a la API.
            authProperties.StoreTokens(new[]
            {
                new AuthenticationToken
                {
                    Name = BearerTokenHandler.NombreToken,
                    Value = usuario.token
                },
                new AuthenticationToken
                {
                    Name = "expires_at",
                    Value = expiraToken.ToString("o")
                }
            });

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

            TempData["Success"] =
                "Cuenta creada correctamente. Le enviamos un correo " +
                "de confirmación a " + model.email + ".";

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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // La API identifica al usuario por su token; no se envía el id.
            var resultado = await _apiContrasenaService
                .CambiarContrasena(model);

            if (!resultado.exitoso)
            {
                ModelState.AddModelError("", resultado.mensaje);

                return View(model);
            }

            // La sesión sigue activa: se muestra la confirmación en la misma página.
            TempData["Success"] = resultado.mensaje;

            return RedirectToAction(nameof(CambiarContrasena));
        }
    }
}