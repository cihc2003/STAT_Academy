using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Seguridad;

namespace STAT_Academy.Tests;

public class SeguridadTests
{
    private static ValidadorSesion Validador(string? clave = null) => new(
        Options.Create(new JwtOptions { Key = clave ?? Convert.ToBase64String(new byte[32]) }),
        new MemoryCache(new MemoryCacheOptions()),
        new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>());

    [Fact]
    public void El_sello_es_estable_para_la_misma_contrasena_y_rol()
    {
        var validador = Validador();

        Assert.Equal(validador.CalcularSello("hash-1", 3), validador.CalcularSello("hash-1", 3));
    }

    [Fact]
    public void El_sello_cambia_con_la_contrasena_o_el_rol()
    {
        var validador = Validador();
        var original = validador.CalcularSello("hash-1", 3);

        Assert.NotEqual(original, validador.CalcularSello("hash-2", 3));
        Assert.NotEqual(original, validador.CalcularSello("hash-1", 1));
    }

    [Fact]
    public void El_sello_depende_de_la_clave_del_servidor()
    {
        var otraClave = Convert.ToBase64String(Enumerable.Repeat((byte)7, 32).ToArray());

        Assert.NotEqual(Validador().CalcularSello("hash-1", 3), Validador(otraClave).CalcularSello("hash-1", 3));
    }

    [Fact]
    public void Limite_de_login_por_correo_sin_distinguir_mayusculas()
    {
        using var limitador = new LimitadorPorCorreo();

        for (var i = 0; i < 10; i++)
            Assert.True(limitador.PermitirLogin("Persona@Ejemplo.com"));

        Assert.False(limitador.PermitirLogin("  persona@ejemplo.com "));
        Assert.True(limitador.PermitirLogin("otra@ejemplo.com"));
    }

    [Fact]
    public void Limite_de_recuperacion_por_correo()
    {
        using var limitador = new LimitadorPorCorreo();

        Assert.True(limitador.PermitirRecuperacion("a@ejemplo.com"));
        Assert.True(limitador.PermitirRecuperacion("a@ejemplo.com"));
        Assert.True(limitador.PermitirRecuperacion("a@ejemplo.com"));
        Assert.False(limitador.PermitirRecuperacion("a@ejemplo.com"));
    }
}
