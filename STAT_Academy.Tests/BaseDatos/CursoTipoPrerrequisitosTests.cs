using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Curso;
using Xunit;

namespace STAT_Academy.Tests.BaseDatos;

/// <summary>
/// Tipo y prerrequisitos del curso: texto libre opcional, con los largos de las columnas
/// (CURSO.Tipo NVARCHAR(50) y CURSO.Prerrequisitos NVARCHAR(500)).
/// </summary>
public sealed class CursoTipoPrerrequisitosTests : IDisposable
{
    private readonly BaseDatosPrueba _bd = new();
    private readonly BlobStorageSimulado _blob = new();
    private readonly List<ApplicationDbContext> _contextos = [];

    public void Dispose()
    {
        foreach (var c in _contextos)
            c.Dispose();
        _bd.Dispose();
    }

    private CursoService Servicio()
    {
        var c = _bd.NuevoContexto();
        _contextos.Add(c);
        return new CursoService(c, _blob, new AuditoriaService(c), NullLogger<CursoService>.Instance);
    }

    private static CursoCreateRequest Solicitud(int tutorId, string? tipo = null, string? prerrequisitos = null) => new()
    {
        fk_tutor = tutorId,
        nombre = "Bioestadística",
        descripcion = "Curso de prueba",
        precio = 10000m,
        fechaInicio = CursoModel.HoyCostaRica.AddDays(7),
        fechaFin = CursoModel.HoyCostaRica.AddDays(35),
        tipo = tipo,
        prerrequisitos = prerrequisitos
    };

    private static List<ValidationResult> Validar(object modelo)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, new ValidationContext(modelo), resultados, validateAllProperties: true);
        return resultados;
    }

    // ------------------------------------------------------------------
    // Validación de la solicitud
    // ------------------------------------------------------------------

    [Fact]
    public void Ambos_campos_son_opcionales()
    {
        Assert.Empty(Validar(Solicitud(1)));
    }

    [Fact]
    public void Acepta_los_largos_maximos_de_las_columnas()
    {
        var solicitud = Solicitud(1, new string('a', 50), new string('b', 500));

        Assert.Empty(Validar(solicitud));
    }

    [Fact]
    public void Rechaza_un_tipo_de_mas_de_50_caracteres()
    {
        var errores = Validar(Solicitud(1, tipo: new string('a', 51)));

        var error = Assert.Single(errores);
        Assert.Contains(nameof(CursoCreateRequest.tipo), error.MemberNames);
        Assert.Equal("El tipo admite como máximo 50 caracteres.", error.ErrorMessage);
    }

    [Fact]
    public void Rechaza_prerrequisitos_de_mas_de_500_caracteres()
    {
        var errores = Validar(Solicitud(1, prerrequisitos: new string('b', 501)));

        var error = Assert.Single(errores);
        Assert.Contains(nameof(CursoCreateRequest.prerrequisitos), error.MemberNames);
        Assert.Equal("Los prerrequisitos admiten como máximo 500 caracteres.", error.ErrorMessage);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  Taller ", "Taller")]
    public void Limpiar_recorta_y_convierte_vacios_en_null(string? entrada, string? esperado)
    {
        Assert.Equal(esperado, CursoCreateRequest.Limpiar(entrada));
    }

    // ------------------------------------------------------------------
    // Lectura y escritura en CursoService
    // ------------------------------------------------------------------

    [Fact]
    public async Task Crear_y_editar_guardan_tipo_y_prerrequisitos()
    {
        var tutor = BaseDatosPrueba.Usuario("tutor@stat.cr", tipo: 2);
        _bd.Sembrar(tutor);

        var creado = await Servicio().CrearAsync(
            Solicitud(tutor.id, "  Virtual ", "Estadística básica.\nManejo de Excel."), creadorId: tutor.id);

        Assert.Equal("Virtual", creado.tipo);
        Assert.Equal("Estadística básica.\nManejo de Excel.", creado.prerrequisitos);

        // Dejarlos en blanco al editar los borra (quedan en NULL, como los cursos anteriores).
        var editado = await Servicio().EditarAsync(creado.id, Solicitud(tutor.id, "Taller", "   "));

        Assert.NotNull(editado);
        Assert.Equal("Taller", editado.tipo);
        Assert.Null(editado.prerrequisitos);

        var guardado = await _bd.NuevoContexto().Curso.AsNoTracking().SingleAsync(c => c.id == creado.id);
        Assert.Equal("Taller", guardado.tipo);
        Assert.Null(guardado.prerrequisitos);
    }

    [Fact]
    public async Task Un_curso_sin_tipo_ni_prerrequisitos_se_lista_con_null()
    {
        var tutor = BaseDatosPrueba.Usuario("tutor@stat.cr", tipo: 2);
        _bd.Sembrar(tutor);
        _bd.Sembrar(BaseDatosPrueba.Curso("Curso existente", tutor.id));

        var curso = Assert.Single(await Servicio().GetAllAsync(esAdmin: true));

        Assert.Null(curso.tipo);
        Assert.Null(curso.prerrequisitos);
    }
}
