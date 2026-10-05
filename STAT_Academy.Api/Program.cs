using STAT_Academy.Api.Data;
using STAT_Academy.Api.Services;
using Microsoft.EntityFrameworkCore;

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
        sql => sql.EnableRetryOnFailure()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "STAT Academy API v1");
        c.ScriptBundlePath = "./swagger-ui-bundle.js?v=10.2.3";
        c.ScriptPresetsPath = "./swagger-ui-standalone-preset.js?v=10.2.3";
        c.StylesPath = "./swagger-ui.css?v=10.2.3";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();