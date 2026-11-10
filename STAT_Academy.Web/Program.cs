using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using STAT_Academy.Web.Controllers;
using STAT_Academy.Web.Services;

var builder = WebApplication.CreateBuilder(args);

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

AddApiClient<CursoService>();
AddApiClient<ApiUserGateway>();
AddApiClient<ApiContrasenaService>();
AddApiClient<ApiUsuarioService>();
AddApiClient<ApiProductoService>();
AddApiClient<ApiProveedorService>();
AddApiClient<ApiCarritoService>();
AddApiClient<ApiCursoService>();
AddApiClient<ApiBlogService>();

// Cliente con nombre para los controladores que usan HttpClient directamente.
builder.Services.AddHttpClient(ApiClient.Nombre, client => client.BaseAddress = apiUri)
    .ConfigurePrimaryHttpMessageHandler(CreateDevelopmentHandler)
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddControllersWithViews();

builder.Services.AddSession();

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
