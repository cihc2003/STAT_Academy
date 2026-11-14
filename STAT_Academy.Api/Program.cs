using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Configuration;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Services;
using STAT_Academy.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuración obligatoria (secretos). Local: user-secrets. Azure: App Settings.
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Falta ConnectionStrings:DefaultConnection. Configúrela con user-secrets o en Azure App Settings.");

var jwtOptions = builder.Configuration.GetSection(JwtOptions.Seccion).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Key))
    throw new InvalidOperationException(
        "Falta Jwt:Key. Configúrela con user-secrets o en Azure App Settings (Jwt__Key).");

var jwtKeyBytes = Convert.FromBase64String(jwtOptions.Key);
if (jwtKeyBytes.Length < 32)
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 bytes (256 bits) en Base64.");

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<CursoService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<ProveedorService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<ProductoService>();
builder.Services.AddScoped<CarritoService>();
builder.Services.AddScoped<EstudianteCursoService>();
builder.Services.AddScoped<TareaService>();
builder.Services.AddScoped<MaterialCursoService>();
builder.Services.AddScoped<BlogService>();
builder.Services.AddScoped<TutorService>();
builder.Services.AddSingleton<BlogContenidoService>();
builder.Services.AddScoped<StockAlertaService>();
builder.Services.AddScoped<CompraService>();
builder.Services.AddScoped<ContrasenaService>();
builder.Services.AddScoped<CorreoService>();

// ---------------------------------------------------------------------------
// Azure Blob Storage (misma configuración que la rama Pruebas_Blob_Storage)
//  - Local: credencial de Visual Studio o "az login" (rol Storage Blob Data Contributor).
//  - Azure: Managed Identity del App Service. No se usan claves de cuenta.
// ---------------------------------------------------------------------------
builder.Services.Configure<BlobStorageOptions>(
    builder.Configuration.GetSection(BlobStorageOptions.Seccion));

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
    var env = sp.GetRequiredService<IHostEnvironment>();

    if (!Uri.TryCreate(config.AccountUrl?.Trim(), UriKind.Absolute, out var accountUri))
        throw new InvalidOperationException($"BlobStorage:AccountUrl no es válida: '{config.AccountUrl}'");

    var tenantId = string.IsNullOrWhiteSpace(config.TenantId) ? null : config.TenantId.Trim();

    TokenCredential credencial = env.IsDevelopment()
        ? new ChainedTokenCredential(
            new VisualStudioCredential(new VisualStudioCredentialOptions { TenantId = tenantId, ProcessTimeout = TimeSpan.FromSeconds(30) }),
            new AzureCliCredential(new AzureCliCredentialOptions { TenantId = tenantId, ProcessTimeout = TimeSpan.FromSeconds(15) }))
        : string.IsNullOrWhiteSpace(config.ManagedIdentityClientId)
            ? new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)
            : new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(config.ManagedIdentityClientId));

    // Límites para que un problema de red falle rápido en lugar de quedarse "cargando"
    var opcionesCliente = new BlobClientOptions();
    opcionesCliente.Retry.MaxRetries = 2;
    opcionesCliente.Retry.NetworkTimeout = TimeSpan.FromSeconds(30);

    return new BlobServiceClient(accountUri, credencial, opcionesCliente);
});

builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

// ---------------------------------------------------------------------------
// Autenticación JWT
// ---------------------------------------------------------------------------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Seccion));
builder.Services.AddSingleton<TokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTiposJwt.Nombre,
            RoleClaimType = ClaimTiposJwt.Rol
        };
    });

// Seguro por defecto: todo endpoint exige token salvo que lleve [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "STAT Academy API",
        Version = "v1",
        Description = "API for STAT Academy"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Pegue el token obtenido en POST /api/Login"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sql => sql.EnableRetryOnFailure()));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "STAT Academy API v1");
    c.ScriptBundlePath = "./swagger-ui-bundle.js?v=10.2.3";
    c.ScriptPresetsPath = "./swagger-ui-standalone-preset.js?v=10.2.3";
    c.StylesPath = "./swagger-ui.css?v=10.2.3";
});
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
