using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;

namespace STAT_Academy.Tests.BaseDatos;

/// <summary>
/// Base SQLite en memoria con el esquema del modelo de EF (EnsureCreated). Vive mientras
/// la conexión esté abierta: cada prueba crea la suya y la descarta al terminar.
/// SQLite es relacional (traduce LINQ a SQL y aplica restricciones como el índice único
/// del correo), a diferencia del proveedor InMemory de EF.
/// </summary>
public sealed class BaseDatosPrueba : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly DbContextOptions<ApplicationDbContext> _opciones;

    public BaseDatosPrueba()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        _opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_conexion)
            .Options;

        using var contexto = NuevoContexto();
        contexto.Database.EnsureCreated();
    }

    /// <summary>Contexto nuevo sobre la misma base (sirve para comprobar lo que quedó guardado).</summary>
    public ApplicationDbContext NuevoContexto() => new(_opciones);

    public static ValidadorSesion Validador() => new(
        Options.Create(new JwtOptions { Key = Convert.ToBase64String(new byte[32]) }),
        new MemoryCache(new MemoryCacheOptions()),
        new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>());

    /// <summary>Agrega las entidades y guarda; después de esto ya tienen su id.</summary>
    public void Sembrar(params object[] entidades)
    {
        using var contexto = NuevoContexto();
        contexto.AddRange(entidades);
        contexto.SaveChanges();
    }

    public static UsuarioModel Usuario(string email, int tipo = 3, bool activo = true, string? nombre = null) => new()
    {
        email = email,
        nombre = nombre ?? email.Split('@')[0],
        password = "hash",
        estado = activo,
        fk_Tipo_Usuario = tipo
    };

    public static ProductoModel Producto(string nombre, int proveedorId, int stock = 10, int minimo = 2, bool activo = true) => new()
    {
        nombre = nombre,
        categoria = "General",
        descripcion = "Producto de prueba",
        precio_base = 1000m,
        stock = stock,
        min_stock = minimo,
        estado = activo,
        fk_proveedor = proveedorId
    };

    public static CursoModel Curso(string nombre, int tutorId, bool activo = true) => new()
    {
        nombre = nombre,
        descripcion = "Curso de prueba",
        fk_tutor = tutorId,
        fk_creador = tutorId,
        precio = 0m,
        estado = activo,
        fechaInicio = CursoModel.HoyCostaRica.AddDays(-7),
        fechaFin = CursoModel.HoyCostaRica.AddDays(21)
    };

    public void Dispose() => _conexion.Dispose();
}
