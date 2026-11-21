using STAT_Academy.Api.Models;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Tests;

public class NotaFinalCalculoTests
{
    private static readonly DateTime Ahora = new(2026, 11, 15, 12, 0, 0);

    private static TareaModel Tarea(int id, DateTime? limite) => new() { ID = id, Fecha_Limite = limite, Estado = true };

    private static EntregaTareaModel Entrega(int id, int tareaId, decimal? nota = null, bool entregada = true) => new()
    {
        id = id,
        fk_Tarea = tareaId,
        calificacion = nota,
        fecha_Entrega = entregada ? Ahora.AddDays(-1) : null
    };

    [Fact]
    public void Promedia_calificadas_y_cuenta_cero_las_vencidas_sin_entregar()
    {
        var tareas = new[]
        {
            Tarea(1, Ahora.AddDays(-5)),   // calificada 90
            Tarea(2, Ahora.AddDays(-2)),   // vencida sin entregar → 0
            Tarea(3, Ahora.AddDays(3)),    // abierta → no cuenta
            Tarea(4, Ahora.AddDays(-1)),   // entregada sin calificar → no cuenta
        };
        var entregas = new[] { Entrega(10, 1, 90), Entrega(11, 4) };

        var nota = NotaFinalCalculo.Calcular(tareas, entregas, ajuste: null, ahora: Ahora);

        Assert.Equal(45m, nota.calculada);
        Assert.Equal(2, nota.tareasConsideradas);
        Assert.Equal(4, nota.tareasTotales);
        Assert.Equal(1, nota.porCalificar);
        Assert.Null(nota.ajustada);
    }

    [Fact]
    public void Usa_la_entrega_mas_reciente_de_cada_tarea()
    {
        var tareas = new[] { Tarea(1, Ahora.AddDays(-5)) };
        var entregas = new[] { Entrega(10, 1, 50), Entrega(12, 1, 80) };

        var nota = NotaFinalCalculo.Calcular(tareas, entregas, null, Ahora);

        Assert.Equal(80m, nota.calculada);
    }

    [Fact]
    public void Sin_tareas_que_cuenten_no_hay_nota_calculada()
    {
        var nota = NotaFinalCalculo.Calcular([Tarea(1, Ahora.AddDays(2))], [], null, Ahora);

        Assert.Null(nota.calculada);
        Assert.Equal(0, nota.tareasConsideradas);
    }

    [Fact]
    public void El_ajuste_del_tutor_se_informa_aparte_de_la_calculada()
    {
        var ajuste = new NotaFinalModel { nota = 95m, comentario = "Participación" };

        var nota = NotaFinalCalculo.Calcular([Tarea(1, Ahora.AddDays(-1))], [], ajuste, Ahora);

        Assert.Equal(0m, nota.calculada);
        Assert.Equal(95m, nota.ajustada);
        Assert.Equal("Participación", nota.comentario);
    }

    [Theory]
    [InlineData("2026-11-01", "2026-11-30", "2026-10-31", 0)]
    [InlineData("2026-11-01", "2026-11-30", "2026-11-01", 3)]
    [InlineData("2026-11-01", "2026-11-30", "2026-11-15", 50)]
    [InlineData("2026-11-01", "2026-11-30", "2026-11-30", 100)]
    public void Progreso_del_curso_segun_las_fechas(string inicio, string fin, string hoy, int esperado)
    {
        Assert.Equal(esperado, CursoModel.ProgresoDe(DateTime.Parse(inicio), DateTime.Parse(fin), DateTime.Parse(hoy)));
    }
}
