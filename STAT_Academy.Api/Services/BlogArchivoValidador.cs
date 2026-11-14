namespace STAT_Academy.Api.Services
{
    /// <summary>
    /// Reglas para los adjuntos del blog. El tipo se decide por la extensión Y por la
    /// firma real del archivo (primeros bytes), no por el Content-Type del navegador,
    /// para que nadie pueda subir, por ejemplo, un HTML renombrado a .jpg.
    /// </summary>
    public static class BlogArchivoValidador
    {
        public const long MaxBytesImagen = 5 * 1024 * 1024;     // 5 MB
        public const long MaxBytesDocumento = 10 * 1024 * 1024; // 10 MB
        public const int MaxArchivosPorSubida = 10;
        public const int MaxArchivosPorEntrada = 20;

        /// <summary>Tamaño máximo de una petición de subida (10 archivos de hasta 10 MB + margen).</summary>
        public const long MaxBytesPeticion = 110 * 1024 * 1024;

        private sealed record TipoPermitido(string Mime, bool EsImagen, Func<byte[], bool> FirmaValida);

        private static readonly Func<byte[], bool> Zip =
            b => Empieza(b, 0x50, 0x4B, 0x03, 0x04); // docx/xlsx/pptx son ZIP

        private static readonly Dictionary<string, TipoPermitido> Permitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            // Imágenes (SVG excluido a propósito: puede contener scripts)
            [".jpg"] = new("image/jpeg", true, b => Empieza(b, 0xFF, 0xD8, 0xFF)),
            [".jpeg"] = new("image/jpeg", true, b => Empieza(b, 0xFF, 0xD8, 0xFF)),
            [".png"] = new("image/png", true, b => Empieza(b, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)),
            [".gif"] = new("image/gif", true, b => Empieza(b, 0x47, 0x49, 0x46, 0x38)),
            [".webp"] = new("image/webp", true, b => Empieza(b, 0x52, 0x49, 0x46, 0x46)
                                                    && b.Length > 12 && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50),

            // Documentos
            [".pdf"] = new("application/pdf", false, b => Empieza(b, 0x25, 0x50, 0x44, 0x46)),
            [".doc"] = new("application/msword", false, b => Empieza(b, 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1)),
            [".docx"] = new("application/vnd.openxmlformats-officedocument.wordprocessingml.document", false, Zip),
            [".xlsx"] = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", false, Zip),
            [".pptx"] = new("application/vnd.openxmlformats-officedocument.presentationml.presentation", false, Zip),
            [".txt"] = new("text/plain; charset=utf-8", false, b => !b.Contains((byte)0)),
            [".csv"] = new("text/csv; charset=utf-8", false, b => !b.Contains((byte)0)),
        };

        public static string ExtensionesPermitidas =>
            string.Join(", ", Permitidos.Keys.Where(k => k != ".jpeg"));

        public sealed record ArchivoValidado(
            string NombreOriginal,
            string Extension,
            string Mime,
            bool EsImagen,
            byte[] Contenido);

        /// <summary>Lee y valida un archivo; devuelve el error o el archivo listo para subir.</summary>
        public static async Task<(ArchivoValidado? archivo, string? error)> Validar(IFormFile archivo)
        {
            var nombre = Path.GetFileName(archivo.FileName);
            var extension = Path.GetExtension(nombre);

            if (archivo.Length == 0)
                return (null, $"'{nombre}' está vacío.");

            if (!Permitidos.TryGetValue(extension, out var tipo))
                return (null, $"'{nombre}': tipo no permitido. Se aceptan {ExtensionesPermitidas}.");

            var maximo = tipo.EsImagen ? MaxBytesImagen : MaxBytesDocumento;
            if (archivo.Length > maximo)
                return (null, $"'{nombre}' supera el máximo de {maximo / (1024 * 1024)} MB.");

            using var memoria = new MemoryStream();
            await archivo.CopyToAsync(memoria);
            var bytes = memoria.ToArray();

            if (!tipo.FirmaValida(bytes))
                return (null, $"'{nombre}': el contenido no corresponde a un archivo {extension}.");

            // Nombre visible acotado a la columna (varchar 255), sin rutas.
            if (nombre.Length > 255)
                nombre = nombre[..(255 - extension.Length)] + extension;

            return (new ArchivoValidado(nombre, extension.ToLowerInvariant(), tipo.Mime, tipo.EsImagen, bytes), null);
        }

        private static bool Empieza(byte[] datos, params byte[] firma) =>
            datos.Length >= firma.Length && datos.AsSpan(0, firma.Length).SequenceEqual(firma);
    }
}
