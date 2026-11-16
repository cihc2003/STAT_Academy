using Microsoft.AspNetCore.Http;
using STAT_Academy.Api.Storage;

namespace STAT_Academy.Tests.BaseDatos;

/// <summary>
/// Blob Storage en memoria para las pruebas: guarda los archivos en un diccionario
/// y permite simular fallos de subida.
/// </summary>
public sealed class BlobStorageSimulado : IBlobStorageService
{
    public Dictionary<string, (ContenedorBlob contenedor, byte[] contenido, string contentType)> Archivos { get; } = [];

    /// <summary>Si es true, las subidas lanzan una excepción (como un fallo de red).</summary>
    public bool FallarSubidas { get; set; }

    private int _siguiente;

    public ContenedorBlob? DetectarContenedor(string nombreArchivo) =>
        Path.GetExtension(nombreArchivo).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" => ContenedorBlob.Imagenes,
            ".pdf" or ".docx" or ".xlsx" or ".pptx" or ".txt" or ".csv" => ContenedorBlob.Documentos,
            _ => null
        };

    public async Task<ArchivoSubido> SubirAsync(IFormFile archivo, ContenedorBlob contenedor, string carpeta, CancellationToken ct = default)
    {
        Fallar();
        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria, ct);
        var nombre = $"{carpeta}/{++_siguiente}{Path.GetExtension(archivo.FileName).ToLowerInvariant()}";
        var tipo = string.IsNullOrEmpty(archivo.ContentType) ? "application/octet-stream" : archivo.ContentType;
        Archivos[nombre] = (contenedor, memoria.ToArray(), tipo);
        return new ArchivoSubido(nombre, Path.GetFileName(archivo.FileName), tipo, archivo.Length);
    }

    public Task<string> SubirPrivadoAsync(byte[] contenido, string extension, string contentType, string carpeta, CancellationToken ct = default)
    {
        Fallar();
        var nombre = $"{carpeta}/{++_siguiente}{extension}";
        Archivos[nombre] = (ContenedorBlob.Documentos, contenido, contentType);
        return Task.FromResult(nombre);
    }

    public string ObtenerUrlPublica(string blobName) => $"https://pruebas.blob.core.windows.net/imagenes/{blobName}";

    public Task<ArchivoDescarga> DescargarAsync(ContenedorBlob contenedor, string blobName, CancellationToken ct = default)
    {
        if (!Archivos.TryGetValue(blobName, out var archivo) || archivo.contenedor != contenedor)
            throw new Azure.RequestFailedException(404, "No existe");

        return Task.FromResult(new ArchivoDescarga(new MemoryStream(archivo.contenido), archivo.contentType));
    }

    public Task<Uri> GenerarEnlaceTemporalAsync(string blobName, string nombreDescarga, TimeSpan duracion, CancellationToken ct = default) =>
        Task.FromResult(new Uri(ObtenerUrlPublica(blobName) + "?sas=prueba"));

    public Task EliminarAsync(ContenedorBlob contenedor, string blobName, CancellationToken ct = default)
    {
        Archivos.Remove(blobName);
        return Task.CompletedTask;
    }

    private void Fallar()
    {
        if (FallarSubidas)
            throw new IOException("Fallo simulado de Blob Storage.");
    }

    // Archivos mínimos con firma válida (BlogArchivoValidador revisa los primeros bytes).
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13];
    public static readonly byte[] Pdf = "%PDF-1.4\n%prueba\n"u8.ToArray();

    public static IFormFile Archivo(string nombre, byte[] contenido, string? contentType = null) =>
        new FormFile(new MemoryStream(contenido), 0, contenido.Length, "archivo", nombre)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType ?? (nombre.EndsWith(".png") ? "image/png" : "application/pdf")
        };
}
