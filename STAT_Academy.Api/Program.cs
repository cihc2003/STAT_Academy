using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using STAT_Academy.Api.Configuration;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Services;
using STAT_Academy.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<ContrasenaService>();
builder.Services.AddScoped<CorreoService>();
builder.Services.AddScoped<CambioCorreoService>();
builder.Services.AddScoped<SupabaseStorageService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IProductoImagenService, ProductoImagenService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "STAT Academy API",
        Version = "v1",
        Description = "API for STAT Academy"
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// ===================== Azure Blob Storage =====================
builder.Services.Configure<BlobStorageOptions>(
    builder.Configuration.GetSection(BlobStorageOptions.Seccion));

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
    var env = sp.GetRequiredService<IHostEnvironment>();

    if (!Uri.TryCreate(config.AccountUrl?.Trim(), UriKind.Absolute, out var accountUri))
        throw new InvalidOperationException($"BlobStorage:AccountUrl no es válida: '{config.AccountUrl}'");

    TokenCredential credencial;

    if (env.IsDevelopment())
    {
        // LOCAL: tu usuario (cuenta de Visual Studio o "az login"). Nunca Managed Identity.
        Console.WriteLine(">>> Blob Storage: credencial de DESARROLLO (Visual Studio / Azure CLI)");

        credencial = new ChainedTokenCredential(
            new VisualStudioCredential(new VisualStudioCredentialOptions
            {
                ProcessTimeout = TimeSpan.FromSeconds(15)
            }),
            new AzureCliCredential(new AzureCliCredentialOptions
            {
                ProcessTimeout = TimeSpan.FromSeconds(15)
            }));
    }
    else
    {
        // AZURE: Managed Identity del App Service
        Console.WriteLine(">>> Blob Storage: MANAGED IDENTITY");

        credencial = string.IsNullOrWhiteSpace(config.ManagedIdentityClientId)
            ? new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)
            : new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(config.ManagedIdentityClientId));
    }

    // Límites para que un problema de red falle rápido en lugar de quedarse "cargando"
    var opcionesCliente = new BlobClientOptions();
    opcionesCliente.Retry.MaxRetries = 2;
    opcionesCliente.Retry.NetworkTimeout = TimeSpan.FromSeconds(30);

    return new BlobServiceClient(accountUri, credencial, opcionesCliente);
});


// ==============================================================

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

app.UseAuthorization();

app.MapControllers();

app.Run();