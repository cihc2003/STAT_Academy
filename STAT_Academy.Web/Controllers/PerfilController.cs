using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Models.Contrasena;
using STAT_Academy.Web.Models.Perfil;
using STAT_Academy.Web.Services;
using System.Security.Claims;

namespace STAT_Academy.Web.Controllers
{
    /// <summary>
    /// Perfil del usuario autenticado (cualquier rol): sus datos, el cambio de nombre y de contraseña.
    /// "Facturas y pedidos" (clientes) vive en FacturasController y comparte el menú lateral.
    /// </summary>
    [Authorize]
    public class PerfilController : Controller
    {
        private readonly ApiPerfilService _apiPerfil;
        private readonly ApiContrasenaService _apiContrasena;

        public PerfilController(ApiPerfilService apiPerfil, ApiContrasenaService apiContrasena)
        {
            _apiPerfil = apiPerfil;
            _apiContrasena = apiContrasena;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var datos = await _apiPerfil.Obtener();
            if (datos == null)
                return NotFound();

            return View(new PerfilViewModel
            {
                Datos = datos,
                Formulario = new EditarPerfilViewModel { nombre = datos.nombre }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind(Prefix = "Formulario")] EditarPerfilViewModel formulario)
        {
            if (!ModelState.IsValid)
                return await VolverConErrores(formulario);

            var (perfil, mensaje) = await _apiPerfil.ActualizarNombre(formulario.nombre);

            if (perfil == null)
            {
                ModelState.AddModelError("", mensaje);
                return await VolverConErrores(formulario);
            }

            // El nombre del encabezado sale del claim: se vuelve a emitir la cookie con el mismo token.
            if (!await SesionWeb.ActualizarNombreAsync(HttpContext, perfil.nombre))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["Success"] = mensaje + " Inicie sesión de nuevo para continuar.";
                return RedirectToAction("Login", "Cuenta");
            }

            TempData["Success"] = mensaje;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Contrasena()
        {
            return View(new CambiarContrasenaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contrasena(CambiarContrasenaViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // La API identifica al usuario por su token; no se envía el id.
            var resultado = await _apiContrasena.CambiarContrasena(model);

            if (!resultado.exitoso)
            {
                ModelState.AddModelError("", resultado.mensaje);
                return View(model);
            }

            // El cambio revoca el token anterior en la API: se guarda el nuevo para que la sesión
            // siga activa. Si no llegó, se pide iniciar sesión de nuevo.
            if (resultado.token == null || resultado.expiraEn == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["Success"] = resultado.mensaje + " Inicie sesión con la contraseña nueva.";
                return RedirectToAction("Login", "Cuenta");
            }

            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                User.Claims, CookieAuthenticationDefaults.AuthenticationScheme));
            await SesionWeb.IniciarAsync(HttpContext, principal, resultado.token, resultado.expiraEn.Value);

            TempData["Success"] = resultado.mensaje;
            return RedirectToAction(nameof(Contrasena));
        }

        private async Task<IActionResult> VolverConErrores(EditarPerfilViewModel formulario)
        {
            var datos = await _apiPerfil.Obtener();
            if (datos == null)
                return NotFound();

            return View(nameof(Index), new PerfilViewModel { Datos = datos, Formulario = formulario });
        }
    }
}
