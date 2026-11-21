using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using STAT_Academy.Web.Models;
using STAT_Academy.Web.Seguridad;
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
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(LimitesSolicitudesWeb.Login)]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            // Solo direcciones de este sitio (evita redirecciones abiertas).
            returnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (usuario, error) = await _apiUserGateway.LoginAsync(model);

            if (usuario == null)
            {
                ModelState.AddModelError(
                    "",
                    error ?? "Usuario o contraseña incorrectos."
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

            await IniciarSesionAsync(new ClaimsPrincipal(identity), usuario.token, usuario.expiraEn);

            // Volver a la página que pedía iniciar sesión (p. ej. tras vencer la sesión).
            if (returnUrl != null)
                return LocalRedirect(returnUrl);

            // El tutor entra directo a su panel de cursos asignados.
            if (rol == "Tutor")
                return RedirectToAction("Index", "Tutor");

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

        /// <summary>Destino de la cookie cuando el usuario no tiene el rol necesario (AccessDeniedPath).</summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            var modelo = ErrorViewModel.ParaCodigo(StatusCodes.Status403Forbidden, HttpContext.TraceIdentifier);

            if (User.Identity?.IsAuthenticated != true)
            {
                modelo.MostrarIngresar = true;
                modelo.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
            }

            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View("Error", modelo);
        }

        [HttpGet]
        public IActionResult Registrar()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(LimitesSolicitudesWeb.Cuenta)]
        public async Task<IActionResult> Registrar(
            RegistroViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // La API recibe los datos y la versión de los términos aceptados; la confirmación no se envía.
            var (resultado, mensajeError) = await _apiUsuarioService.RegistrarUsuarioAsync(
                new RegisterUsuarioRequest
                {
                    nombre = model.nombre.Trim(),
                    email = model.email.Trim(),
                    password = model.password,
                    aceptaTerminos = model.aceptaTerminos,
                    versionTerminos = TerminosVigentes.Version,
                }
            );

            if (resultado == null)
            {
                ModelState.AddModelError(
                    "",
                    string.IsNullOrWhiteSpace(mensajeError) || mensajeError == "No se pudo procesar la solicitud."
                        ? "No se pudo registrar el usuario. Es posible que el correo ya esté registrado."
                        : mensajeError
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
        [EnableRateLimiting(LimitesSolicitudesWeb.Cuenta)]
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
        [EnableRateLimiting(LimitesSolicitudesWeb.Cuenta)]
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

        /// <summary>Dirección anterior: el cambio de contraseña ahora vive en el perfil (PerfilController).</summary>
        [Authorize]
        [HttpGet]
        public IActionResult CambiarContrasena()
        {
            return RedirectToAction("Contrasena", "Perfil");
        }

        private Task IniciarSesionAsync(ClaimsPrincipal principal, string token, DateTime expiraEnUtc) =>
            SesionWeb.IniciarAsync(HttpContext, principal, token, expiraEnUtc);
    }
}