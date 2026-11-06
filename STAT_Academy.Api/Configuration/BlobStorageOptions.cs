namespace STAT_Academy.Api.Configuration;

public sealed class BlobStorageOptions
{
    public const string Seccion = "BlobStorage";

    public string AccountUrl { get; set; } = "";
    public string ContenedorImagenes { get; set; } = "imagenes";
    public string ContenedorDocumentos { get; set; } = "documentos";
    public string? ManagedIdentityClientId { get; set; }
    public int MaxImagenMB { get; set; } = 5;
    public int MaxDocumentoMB { get; set; } = 20;
}