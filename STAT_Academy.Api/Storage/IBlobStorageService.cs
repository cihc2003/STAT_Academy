namespace STAT_Academy.Api.Storage;

public interface IBlobStorageService
{
    /// Indica a qué contenedor va según la extensión (.jpg → Imagenes, .pdf → Documentos, otra → null).
    ContenedorBlob? DetectarContenedor(string nombreArchivo);

    /// Valida y sube el archivo. Devuelve los datos para guardar en la BD.
    Task<ArchivoSubido> SubirAsync(IFormFile archivo, ContenedorBlob contenedor, string carpeta, CancellationToken ct = default);

    /// URL pública de una imagen (solo contenedor público).
    string ObtenerUrlPublica(string blobName);

    /// Descarga un documento privado (la API hace de intermediaria).
    Task<ArchivoDescarga> DescargarAsync(ContenedorBlob contenedor, string blobName, CancellationToken ct = default);

    /// Enlace temporal de solo lectura a un documento privado (User Delegation SAS).
    Task<Uri> GenerarEnlaceTemporalAsync(string blobName, string nombreDescarga, TimeSpan duracion, CancellationToken ct = default);

    /// Borra el archivo físico (si no existe, no falla).
    Task EliminarAsync(ContenedorBlob contenedor, string blobName, CancellationToken ct = default);
}