namespace STAT_Academy.Api.Configuration;

public sealed class BlobStorageOptions
{
    public const string Seccion = "BlobStorage";

    public string AccountUrl { get; set; } = "";
    public string ContenedorImagenes { get; set; } = "imagenes";
    public string ContenedorDocumentos { get; set; } = "documentos";
    public string? ManagedIdentityClientId { get; set; }

    /// <summary>
    /// Tenant (directorio de Entra ID) de la cuenta de almacenamiento. Necesario en local con
    /// cuentas personales (live.com/outlook.com): sin él, Visual Studio pide el token a otro
    /// tenant y falla con AADSTS50020.
    /// </summary>
    public string? TenantId { get; set; }
    public int MaxImagenMB { get; set; } = 5;
    public int MaxDocumentoMB { get; set; } = 20;
}