namespace STAT_Academy.Api.Storage;

/// <summary>A qué contenedor va un archivo.</summary>
public enum ContenedorBlob
{
    Imagenes,    // público
    Documentos   // privado
}

/// <summary>Resultado de subir un archivo: lo que luego guardas en la tabla.</summary>
public sealed record ArchivoSubido(
    string BlobName,        // va en Imagen_URL / Archivo_URL  → "productos/15/3f2a...webp"
    string NombreOriginal,  // va en Nombre_Archivo            → "foto camiseta.webp"
    string ContentType,     // va en Tipo_Archivo              → "image/webp"
    long TamanoBytes);

/// <summary>Archivo descargado desde el contenedor privado.</summary>
public sealed record ArchivoDescarga(Stream Contenido, string ContentType);

/// <summary>Se lanza cuando el archivo no es válido (extensión, tamaño, vacío). El controller la convierte en 400.</summary>
public sealed class ArchivoInvalidoException(string mensaje) : Exception(mensaje);