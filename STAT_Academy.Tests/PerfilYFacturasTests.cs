using STAT_Academy.Api.Models;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Factura;

namespace STAT_Academy.Tests;

public class NombreUsuarioTests
{
    [Fact]
    public void Quita_espacios_de_los_extremos_y_junta_los_repetidos()
    {
        var (nombre, error) = NombreUsuario.Normalizar("  María   José \t Rojas  ");

        Assert.Null(error);
        Assert.Equal("María José Rojas", nombre);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Exige_un_nombre(string? texto)
    {
        var (nombre, error) = NombreUsuario.Normalizar(texto);

        Assert.Null(nombre);
        Assert.Equal("El nombre es obligatorio.", error);
    }

    [Fact]
    public void Rechaza_un_nombre_de_un_solo_caracter_aunque_traiga_espacios()
    {
        var (nombre, error) = NombreUsuario.Normalizar("  A  ");

        Assert.Null(nombre);
        Assert.Contains("entre 2 y 150", error);
    }

    [Fact]
    public void Acepta_150_caracteres_y_rechaza_151()
    {
        Assert.NotNull(NombreUsuario.Normalizar(new string('a', 150)).nombre);
        Assert.Null(NombreUsuario.Normalizar(new string('a', 151)).nombre);
    }

    [Fact]
    public void Rechaza_caracteres_de_control()
    {
        var (nombre, error) = NombreUsuario.Normalizar("Ana\u0000Mora");

        Assert.Null(nombre);
        Assert.Contains("no permitidos", error);
    }
}

public class PaginaConsultaTests
{
    [Theory]
    [InlineData(1, 10, 0, 1, 10, 1)]     // sin resultados: una página vacía
    [InlineData(0, 10, 25, 1, 10, 3)]    // página menor que 1
    [InlineData(9, 10, 25, 3, 10, 3)]    // página después de la última
    [InlineData(2, 0, 5, 2, 1, 5)]       // tamaño mínimo 1
    [InlineData(1, 500, 250, 1, 100, 3)] // tamaño máximo
    public void Ajusta_pagina_y_tamano(int pagina, int tamano, int total, int paginaEsperada, int tamanoEsperado, int totalPaginas)
    {
        var resultado = PaginaConsulta.Normalizar(pagina, tamano, total, 100);

        Assert.Equal((paginaEsperada, tamanoEsperado, totalPaginas), resultado);
    }
}

public class OrdenFacturasTests
{
    private static readonly List<FacturaModel> Facturas =
    [
        new() { id = 1, fecha = new DateTime(2026, 1, 10), total = 5000 },
        new() { id = 2, fecha = new DateTime(2026, 3, 5), total = 20000 },
        new() { id = 3, fecha = new DateTime(2026, 2, 1), total = 12000 },
        new() { id = 4, fecha = new DateTime(2026, 3, 5), total = 5000 },
    ];

    private static int[] Ids(string? orden) =>
        FacturaService.Ordenar(Facturas.AsQueryable(), orden).Select(f => f.id).ToArray();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("desconocido")]
    [InlineData("recientes")]
    public void Por_defecto_muestra_primero_las_mas_recientes(string? orden)
    {
        // Misma fecha: desempata el id más alto (la compra más nueva).
        Assert.Equal([4, 2, 3, 1], Ids(orden));
    }

    [Fact]
    public void Ordena_de_la_mas_antigua_a_la_mas_reciente()
    {
        Assert.Equal([1, 3, 2, 4], Ids("antiguas"));
    }

    [Fact]
    public void Ordena_por_total_y_con_igual_total_la_mas_reciente_primero()
    {
        Assert.Equal([2, 3, 4, 1], Ids("total_desc"));
        Assert.Equal([4, 1, 3, 2], Ids("TOTAL"));
    }

    [Fact]
    public void Normalizar_devuelve_un_orden_valido()
    {
        Assert.Equal(OrdenFacturas.Recientes, OrdenFacturas.Normalizar("x"));
        Assert.Equal(OrdenFacturas.TotalMayor, OrdenFacturas.Normalizar(" Total_Desc "));
    }
}
