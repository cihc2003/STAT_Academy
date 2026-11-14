using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Models;
using STAT_Academy.Web.Services;

namespace STAT_Academy.Web.Controllers
{
    /// <summary>
    /// Páginas de error amigables. Sin detalles técnicos: solo un código para reportar el problema.
    ///  - /Error          → UseExceptionHandler (excepciones no controladas, 500).
    ///  - /Error/{codigo} → UseStatusCodePagesWithReExecute (404, 403, 405...).
    /// Sin [HttpGet] a propósito: se re-ejecutan con el método original (también POST).
    /// </summary>
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ErrorController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(IHttpClientFactory httpClientFactory, ILogger<ErrorController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [Route("Error")]
        public async Task<IActionResult> Index()
        {
            var traceId = HttpContext.TraceIdentifier;
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            // Sesión vencida fuera de un controlador (p. ej. al dibujar una vista): al login.
            if (feature?.Error is ApiSesionExpiradaException)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["Error"] = "Su sesión expiró. Inicie sesión nuevamente para continuar.";
                return RedirectToAction("Login", "Cuenta");
            }

            // 403/404 de la API que no pasaron por ErroresApiFilter.
            if (feature?.Error is HttpRequestException { StatusCode: System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.NotFound } noPermitido)
            {
                var codigo = (int)noPermitido.StatusCode!;
                Response.StatusCode = codigo;
                return View("Error", ErrorViewModel.ParaCodigo(codigo, traceId));
            }

            if (feature?.Error is { } error)
            {
                var ruta = $"{HttpContext.Request.Method} {feature.Path}";
                _logger.LogError(error, "Error no controlado en {Ruta}. TraceId: {TraceId}", ruta, traceId);

                // La API ya avisa de sus propios errores 500, y si no responde tampoco puede recibir
                // el reporte: aquí solo se reportan los errores que nacen en la Web.
                var esErrorDeLaApi = error is TaskCanceledException
                    || error is HttpRequestException { StatusCode: null or >= System.Net.HttpStatusCode.InternalServerError };
                if (!esErrorDeLaApi)
                    await ReportarALaApi(error, ruta, traceId);
            }

            var modelo = ErrorViewModel.ParaCodigo(500, traceId);

            // API caída o sin respuesta: se explica como "servicio no disponible".
            if (feature?.Error is HttpRequestException { StatusCode: null } or TaskCanceledException)
                modelo = ErrorViewModel.ParaCodigo(503, traceId);

            Response.StatusCode = modelo.Codigo;
            return View("Error", modelo);
        }

        [Route("Error/{codigo:int}")]
        public IActionResult Estado(int codigo)
        {
            if (codigo is < 400 or > 599)
                codigo = 500;

            var original = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            var modelo = ErrorViewModel.ParaCodigo(codigo, HttpContext.TraceIdentifier);

            if (codigo is 401 or 403 && User.Identity?.IsAuthenticated != true)
            {
                modelo.MostrarIngresar = true;
                modelo.ReturnUrl = original?.OriginalPath;
            }

            if (codigo >= 500)
                _logger.LogWarning("Respuesta {Codigo} en {Ruta}. TraceId: {TraceId}", codigo, original?.OriginalPath, HttpContext.TraceIdentifier);

            Response.StatusCode = codigo;
            return View("Error", modelo);
        }

        /// <summary>
        /// Pide a la API que avise a los administradores (allí se limita el envío para evitar spam).
        /// Requiere sesión; un fallo aquí nunca rompe la página de error.
        /// </summary>
        private async Task ReportarALaApi(Exception error, string ruta, string traceId)
        {
            if (User.Identity?.IsAuthenticated != true)
                return;

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var cliente = _httpClientFactory.CreateClient(ApiClient.Nombre);

                using var response = await cliente.PostAsJsonAsync("api/Errores", new
                {
                    tipo = Recortar(error.GetType().FullName ?? error.GetType().Name, 200),
                    mensaje = Recortar(error.Message, 1000),
                    ruta = Recortar(ruta, 300),
                    traceId
                }, cts.Token);

                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("La API no aceptó el reporte del error {TraceId}: {Estado}", traceId, (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo reportar el error {TraceId} a la API.", traceId);
            }
        }

        private static string Recortar(string texto, int maximo) =>
            texto.Length <= maximo ? texto : texto[..maximo];
    }
}
