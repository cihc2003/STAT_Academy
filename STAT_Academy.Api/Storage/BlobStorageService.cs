using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using STAT_Academy.Api.Configuration;

namespace STAT_Academy.Api.Storage;

public sealed class BlobStorageService : IBlobStorageService
{
    // Extensiones permitidas y su Content-Type real (no confiamos en el que manda el navegador)
    private static readonly Dictionary<string, string> TiposImagen = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif"
    };

    private static readonly Dictionary<string, string> TiposDocumento = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".txt"] = "text/plain; charset=utf-8",
        [".csv"] = "text/csv; charset=utf-8"
    };

    private readonly BlobServiceClient _servicio;
    private readonly BlobContainerClient _imagenes;
    private readonly BlobContainerClient _documentos;
    private readonly BlobStorageOptions _opciones;

    public BlobStorageService(BlobServiceClient servicio, IOptions<BlobStorageOptions> opciones)
    {
        _servicio = servicio;
        _opciones = opciones.Value;
        _imagenes = servicio.GetBlobContainerClient(_opciones.ContenedorImagenes);
        _documentos = servicio.GetBlobContainerClient(_opciones.ContenedorDocumentos);
    }

    public ContenedorBlob? DetectarContenedor(string nombreArchivo)
    {
        var ext = Path.GetExtension(nombreArchivo);
        if (TiposImagen.ContainsKey(ext)) return ContenedorBlob.Imagenes;
        if (TiposDocumento.ContainsKey(ext)) return ContenedorBlob.Documentos;
        return null;
    }

    public async Task<ArchivoSubido> SubirAsync(IFormFile archivo, ContenedorBlob contenedor, string carpeta, CancellationToken ct = default)
    {
        // 1. Reglas según el contenedor
        var (tipos, maxMB) = contenedor == ContenedorBlob.Imagenes
            ? (TiposImagen, _opciones.MaxImagenMB)
            : (TiposDocumento, _opciones.MaxDocumentoMB);

        // 2. Validaciones
        if (archivo is null || archivo.Length == 0)
            throw new ArchivoInvalidoException("El archivo está vacío.");

        if (archivo.Length > maxMB * 1024L * 1024L)
            throw new ArchivoInvalidoException($"El archivo supera el máximo de {maxMB} MB.");

        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!tipos.TryGetValue(ext, out var contentType))
            throw new ArchivoInvalidoException($"Extensión no permitida: {ext}. Permitidas: {string.Join(", ", tipos.Keys)}");

        // 3. Nombre único del blob: carpeta/guid.ext (nunca el nombre del usuario)
        var blobName = $"{carpeta.Trim('/').ToLowerInvariant()}/{Guid.NewGuid():N}{ext}";

        // 4. Subir con el Content-Type correcto
        var blob = Contenedor(contenedor).GetBlobClient(blobName);
        await using var stream = archivo.OpenReadStream();
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType,
                // Las imágenes nunca cambian (el nombre es un GUID): el navegador puede cachearlas 1 año
                CacheControl = contenedor == ContenedorBlob.Imagenes ? "public, max-age=31536000" : null
            }
        }, ct);

        return new ArchivoSubido(blobName, Path.GetFileName(archivo.FileName), contentType, archivo.Length);
    }

    public async Task<string> SubirPrivadoAsync(byte[] contenido, string extension, string contentType, string carpeta, CancellationToken ct = default)
    {
        if (contenido is null || contenido.Length == 0)
            throw new ArchivoInvalidoException("El archivo está vacío.");

        var ext = extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
        var blobName = $"{carpeta.Trim('/').ToLowerInvariant()}/{Guid.NewGuid():N}{ext}";

        await _documentos.GetBlobClient(blobName).UploadAsync(new BinaryData(contenido), new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, ct);

        return blobName;
    }

    public string ObtenerUrlPublica(string blobName)
    {
        // Compatibilidad: si en la BD ya había una URL completa, se devuelve tal cual
        if (EsUrlCompleta(blobName)) return blobName;
        return _imagenes.GetBlobClient(blobName).Uri.ToString();
    }

    public async Task<ArchivoDescarga> DescargarAsync(ContenedorBlob contenedor, string blobName, CancellationToken ct = default)
    {
        var blob = Contenedor(contenedor).GetBlobClient(Normalizar(blobName));
        var respuesta = await blob.DownloadStreamingAsync(cancellationToken: ct);
        return new ArchivoDescarga(respuesta.Value.Content, respuesta.Value.Details.ContentType);
    }

    public async Task<Uri> GenerarEnlaceTemporalAsync(string blobName, string nombreDescarga, TimeSpan duracion, CancellationToken ct = default)
    {
        var inicio = DateTimeOffset.UtcNow.AddMinutes(-5); // margen por diferencias de reloj
        var fin = DateTimeOffset.UtcNow.Add(duracion);

        // La Managed Identity pide una "llave de delegación" (no se usan account keys)
        var llave = await _servicio.GetUserDelegationKeyAsync(inicio, fin, ct);

        var blob = _documentos.GetBlobClient(Normalizar(blobName));
        var sas = new BlobSasBuilder
        {
            BlobContainerName = _documentos.Name,
            BlobName = blob.Name,
            Resource = "b",
            StartsOn = inicio,
            ExpiresOn = fin,
            ContentDisposition = $"attachment; filename=\"{nombreDescarga}\""
        };
        sas.SetPermissions(BlobSasPermissions.Read);

        return new BlobUriBuilder(blob.Uri)
        {
            Sas = sas.ToSasQueryParameters(llave.Value, _servicio.AccountName)
        }.ToUri();
    }

    public Task EliminarAsync(ContenedorBlob contenedor, string blobName, CancellationToken ct = default)
        => Contenedor(contenedor).DeleteBlobIfExistsAsync(Normalizar(blobName), DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);

    // ---------- helpers ----------

    private BlobContainerClient Contenedor(ContenedorBlob c)
        => c == ContenedorBlob.Imagenes ? _imagenes : _documentos;

    private static bool EsUrlCompleta(string valor)
        => valor.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || valor.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    // Si la BD tiene una URL completa (datos antiguos), extrae solo el nombre del blob
    private static string Normalizar(string valor)
        => EsUrlCompleta(valor) ? new BlobUriBuilder(new Uri(valor)).BlobName : valor;
}