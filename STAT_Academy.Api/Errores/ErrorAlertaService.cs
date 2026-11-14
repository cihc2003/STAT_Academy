using System.Collections.Concurrent;
using System.Threading.Channels;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Errores
{
    /// <summary>Datos de un error que se avisa a los administradores (sin trazas completas).</summary>
    public sealed record AvisoError(
        string Origen,
        string Tipo,
        string Mensaje,
        string Ruta,
        string TraceId,
        DateTime FechaUtc)
    {
        /// <summary>Veces que el mismo error se repitió sin avisar (por el límite de envío).</summary>
        public int Repeticiones { get; init; }
    }

    /// <summary>
    /// Cola de avisos de errores 500 para los administradores.
    /// Limita el envío para no llenar el buzón:
    ///  - el mismo error (origen + tipo + ruta) se avisa como máximo una vez cada
    ///    <c>Errores:MinutosEntreAvisos</c> minutos (por defecto 15); las repeticiones se cuentan
    ///    y se informan en el siguiente aviso;
    ///  - como máximo <c>Errores:MaxAvisosPorHora</c> correos por hora en total (por defecto 20).
    /// El envío ocurre en segundo plano: un error del correo nunca afecta la respuesta.
    /// </summary>
    public class ErrorAlertaService
    {
        private readonly Channel<AvisoError> _cola = Channel.CreateBounded<AvisoError>(
            new BoundedChannelOptions(50) { FullMode = BoundedChannelFullMode.DropWrite });

        private readonly ConcurrentDictionary<string, Registro> _ultimos = new();
        private readonly Queue<DateTime> _enviosUltimaHora = new();
        private readonly object _candado = new();
        private readonly TimeSpan _intervalo;
        private readonly int _maxPorHora;
        private readonly ILogger<ErrorAlertaService> _logger;

        public ErrorAlertaService(IConfiguration configuration, ILogger<ErrorAlertaService> logger)
        {
            _logger = logger;
            _intervalo = TimeSpan.FromMinutes(Math.Max(1, configuration.GetValue("Errores:MinutosEntreAvisos", 15)));
            _maxPorHora = Math.Max(1, configuration.GetValue("Errores:MaxAvisosPorHora", 20));
        }

        internal ChannelReader<AvisoError> Lector => _cola.Reader;

        /// <summary>Encola el aviso si no se envió uno igual hace poco. Nunca lanza excepciones.</summary>
        public void Avisar(AvisoError aviso)
        {
            try
            {
                var clave = $"{aviso.Origen}|{aviso.Tipo}|{aviso.Ruta}";
                var ahora = DateTime.UtcNow;
                int repeticiones;

                lock (_candado)
                {
                    if (_ultimos.TryGetValue(clave, out var registro) && ahora - registro.UltimoEnvio < _intervalo)
                    {
                        registro.Omitidos++;
                        return;
                    }

                    while (_enviosUltimaHora.Count > 0 && ahora - _enviosUltimaHora.Peek() > TimeSpan.FromHours(1))
                        _enviosUltimaHora.Dequeue();

                    if (_enviosUltimaHora.Count >= _maxPorHora)
                    {
                        _logger.LogWarning("Se alcanzó el límite de {Max} avisos de error por hora; no se envía el aviso {TraceId}.",
                            _maxPorHora, aviso.TraceId);
                        return;
                    }

                    repeticiones = registro?.Omitidos ?? 0;
                    _ultimos[clave] = new Registro { UltimoEnvio = ahora };
                    _enviosUltimaHora.Enqueue(ahora);

                    // Limpieza ocasional para que el diccionario no crezca sin control.
                    if (_ultimos.Count > 500)
                    {
                        foreach (var vencido in _ultimos.Where(r => ahora - r.Value.UltimoEnvio > _intervalo).ToList())
                            _ultimos.TryRemove(vencido.Key, out _);
                    }
                }

                if (!_cola.Writer.TryWrite(aviso with { Repeticiones = repeticiones }))
                    _logger.LogWarning("Cola de avisos de error llena; se descarta el aviso {TraceId}.", aviso.TraceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo encolar el aviso de error {TraceId}.", aviso.TraceId);
            }
        }

        private sealed class Registro
        {
            public DateTime UltimoEnvio { get; init; }
            public int Omitidos { get; set; }
        }
    }

    /// <summary>Envía por correo, en segundo plano, los avisos encolados por <see cref="ErrorAlertaService"/>.</summary>
    public class ErrorAlertaEnvioService : BackgroundService
    {
        private readonly ErrorAlertaService _alertas;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ErrorAlertaEnvioService> _logger;

        public ErrorAlertaEnvioService(
            ErrorAlertaService alertas,
            IServiceScopeFactory scopeFactory,
            ILogger<ErrorAlertaEnvioService> logger)
        {
            _alertas = alertas;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var aviso in _alertas.Lector.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var correo = scope.ServiceProvider.GetRequiredService<CorreoService>();

                        var administradores = context.Usuario
                            .Where(u => u.fk_Tipo_Usuario == 1 && u.estado && u.email != null)
                            .Select(u => u.email!)
                            .ToList();

                        if (administradores.Count == 0)
                        {
                            _logger.LogWarning("Error {TraceId} sin administradores activos a quienes avisar.", aviso.TraceId);
                            continue;
                        }

                        await correo.EnviarAlertaError(administradores, aviso);
                    }
                    catch (Exception ex)
                    {
                        // Si la base o el correo fallan, el error ya quedó en el log.
                        _logger.LogError(ex, "No se pudo enviar el aviso del error {TraceId} a los administradores.", aviso.TraceId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // La aplicación se está deteniendo.
            }
        }
    }
}
