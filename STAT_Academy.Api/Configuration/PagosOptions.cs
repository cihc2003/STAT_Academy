namespace STAT_Academy.Api.Configuration;

/// <summary>
/// Cuenta y teléfono a los que los usuarios pagan (sección "Pagos" de appsettings.json).
/// No son secretos: se muestran a cualquier comprador.
/// </summary>
public sealed class PagosOptions
{
    public const string Seccion = "Pagos";

    public string SinpeTelefono { get; set; } = "";
    public string SinpeNombre { get; set; } = "";
    public string Banco { get; set; } = "";
    public string CuentaIban { get; set; } = "";
    public string CuentaTitular { get; set; } = "";
    public string? CedulaJuridica { get; set; }
}
