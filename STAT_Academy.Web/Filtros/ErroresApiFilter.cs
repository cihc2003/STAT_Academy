using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using STAT_Academy.Web.Services;

namespace STAT_Academy.Web.Filtros
{
    /// <summary>
    /// Traduce los errores de la API que llegan como excepción a una respuesta adecuada:
    ///  - 401 (token vencido): cierra la sesión y lleva al login;
    ///  - 403 / 404: respuesta con ese código, que UseStatusCodePagesWithReExecute muestra con la página de error.
    /// El resto (500, API caída, timeouts) sigue hasta UseExceptionHandler.
    /// </summary>
    public class ErroresApiFilter : IAsyncExceptionFilter
    {
        private readonly ITempDataDictionaryFactory _tempDataFactory;
        private readonly ILogger<ErroresApiFilter> _logger;

        public ErroresApiFilter(ITempDataDictionaryFactory tempDataFactory, ILogger<ErroresApiFilter> logger)
        {
            _tempDataFactory = tempDataFactory;
            _logger = logger;
        }

        public async Task OnExceptionAsync(ExceptionContext context)
        {
            var httpContext = context.HttpContext;

            switch (context.Exception)
            {
                case ApiSesionExpiradaException:
                    _logger.LogInformation("La API rechazó el token de {Usuario}; se cierra la sesión.", httpContext.User.Identity?.Name);

                    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    var tempData = _tempDataFactory.GetTempData(httpContext);
                    tempData["Error"] = "Su sesión expiró. Inicie sesión nuevamente para continuar.";
                    tempData.Save();

                    // Tras el login se vuelve a la página (solo GET: un formulario no se puede repetir).
                    var returnUrl = HttpMethods.IsGet(httpContext.Request.Method)
                        ? httpContext.Request.PathBase + httpContext.Request.Path + httpContext.Request.QueryString
                        : null;

                    context.Result = new RedirectToActionResult("Login", "Cuenta", new { returnUrl });
                    context.ExceptionHandled = true;
                    break;

                case HttpRequestException { StatusCode: HttpStatusCode.Forbidden or HttpStatusCode.NotFound } ex:
                    _logger.LogWarning("La API respondió {Estado} en {Ruta}.", (int)ex.StatusCode!, httpContext.Request.Path);

                    context.Result = new StatusCodeResult((int)ex.StatusCode!);
                    context.ExceptionHandled = true;
                    break;
            }
        }
    }
}
