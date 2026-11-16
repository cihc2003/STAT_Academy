using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using STAT_Academy.Web.Controllers;
using STAT_Academy.Web.Filtros;
using STAT_Academy.Web.Seguridad;
using STAT_Academy.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Cultura fija, sin depender del servidor (Azure suele estar en inglés y un equipo local en español):
// español de Costa Rica para fechas y textos, pero con PUNTO decimal, que es lo que envían los
// campos <input type="number">. Sin esto, "45.5" podría leerse como 455 en un servidor en español.
var cultura = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.GetCultureInfo("es-CR").Clone();
cultura.NumberFormat.NumberDecimalSeparator = ".";
cultura.NumberFormat.NumberGroupSeparator = ",";
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultura;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultura;

var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
    ?? throw new InvalidOperationException("Falta ApiSettings:BaseUrl en la configuración.");
if (!apiBaseUrl.EndsWith('/')) apiBaseUrl += "/";
var apiUri = new Uri(apiBaseUrl);

HttpMessageHandler CreateDevelopmentHandler()
{
    var handler = new HttpClientHandler();

    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }

    return handler;
}

// Todos los clientes de la API comparten base URL y reenvían el JWT del usuario.
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BearerTokenHandler>();

void AddApiClient<TClient>() where TClient : class =>
    builder.Services.AddHttpClient<TClient>(client => client.BaseAddress = apiUri)
        .ConfigurePrimaryHttpMessageHandler(CreateDevelopmentHandler)
        .AddHttpMessageHandler<BearerTokenHandler>();

AddApiClient<ApiUserGateway>();
AddApiClient<ApiContrasenaService>();
AddApiClient<ApiUsuarioService>();
AddApiClient<ApiProductoService>();
AddApiClient<ApiProveedorService>();
AddApiClient<ApiCarritoService>();
AddApiClient<ApiCursoService>();
AddApiClient<ApiBlogService>();
AddApiClient<ApiTutorService>();
AddApiClient<ApiFacturaService>();
AddApiClient<ApiPerfilService>();

// Cliente con nombre para los controladores que usan HttpClient directamente.
builder.Services.AddHttpClient(ApiClient.Nombre, client => client.BaseAddress = apiUri)
    .ConfigurePrimaryHttpMessageHandler(CreateDevelopmentHandler)
    .AddHttpMessageHandler<BearerTokenHandler>();

// 401/403/404 de la API → login o página de error (ver ErroresApiFilter).
builder.Services.AddControllersWithViews(options => options.Filters.Add<ErroresApiFilter>());

// Límites por IP en login, registro y recuperación de contraseña.
builder.Services.AddLimitesSolicitudesWeb();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Login";
        options.AccessDeniedPath = "/Cuenta/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        // En Azure la cookie (que lleva el JWT) solo viaja por HTTPS; en local se permite http://localhost.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        // Si el JWT de la API ya expiró, la sesión de la Web tampoco sirve:
        // se cierra y el usuario vuelve a iniciar sesión.
        options.Events.OnValidatePrincipal = async context =>
        {
            var expiraTexto = context.Properties.GetTokenValue("expires_at");

            if (!DateTimeOffset.TryParse(expiraTexto, out var expira)
                || expira <= DateTimeOffset.UtcNow)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SoloAdmin", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("SoloTutor", policy =>
        policy.RequireRole("Tutor"));

    options.AddPolicy("SoloCliente", policy =>
        policy.RequireRole("Cliente"));
});


var app = builder.Build();

// Páginas de error propias en todos los entornos: el usuario nunca ve trazas.
// El detalle queda en el log con el mismo código (TraceIdentifier) que muestra la página.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    ExceptionHandlingPath = "/Error",
    // ErrorController ya registra la excepción con su código; se evita el log duplicado.
    SuppressDiagnosticsCallback = _ => true,
    // ErrorController responde 404 cuando la API dice que el recurso no existe.
    AllowStatusCode404Response = true
});
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Encabezados de seguridad en todas las respuestas (una acción puede fijar los suyos antes).
app.Use(async (context, next) =>
{
    var encabezados = context.Response.Headers;
    encabezados.XContentTypeOptions = "nosniff";
    // Ningún otro sitio puede mostrar estas páginas en un marco (clickjacking).
    encabezados.XFrameOptions = "SAMEORIGIN";
    encabezados["Referrer-Policy"] = "strict-origin-when-cross-origin";

    // CSP solo en páginas HTML (no en PDF ni descargas) y sin pisar la de una acción
    // (p. ej. el comprobante de pago, que fija la suya con "sandbox").
    context.Response.OnStarting(() =>
    {
        if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true
            && !encabezados.ContainsKey("Content-Security-Policy"))
        {
            encabezados["Content-Security-Policy"] = PoliticaContenido;
        }
        return Task.CompletedTask;
    });

    await next();
});

// 404, 403, 405... sin contenido → /Error/{código}, conservando la URL original.
app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program
{
    /// <summary>
    /// Content-Security-Policy de las páginas. Sin JavaScript inline: los comportamientos van en site.js
    /// con atributos data-*. Los estilos inline se permiten (atributos style= de las vistas y colores del
    /// contenido del blog, ya saneado en la API). Externos: Quill (jsDelivr), imágenes de Azure Blob
    /// Storage y el mapa de Google en Contacto.
    /// </summary>
    private const string PoliticaContenido =
        "default-src 'self'; " +
        "script-src 'self' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: blob: https://*.blob.core.windows.net; " +
        "font-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-src https://www.google.com https://maps.google.com; " +
        "frame-ancestors 'self'; " +
        "form-action 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'";
}
