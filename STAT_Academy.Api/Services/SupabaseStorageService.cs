using Supabase;
using Supabase.Storage;
using System.Text;

namespace STAT_Academy.Api.Services
{
    public class SupabaseStorageService
    {
        private readonly Supabase.Client _supabase;
        private readonly string _bucket;

        public SupabaseStorageService(IConfiguration configuration)
        {
            var url = configuration["Supabase:Url"];
            var key = configuration["Supabase:Key"];
            _bucket = configuration["Supabase:Bucket"] ?? "stat_academi";

            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("No se configuró Supabase:Url");

            if (string.IsNullOrWhiteSpace(key))
                throw new Exception("No se configuró Supabase:Key");

            _supabase = new Supabase.Client(
                url,
                key,
                new SupabaseOptions
                {
                    AutoRefreshToken = false,
                    AutoConnectRealtime = false
                }
            );
        }

        public async Task<string> SubirArchivo(
            IFormFile archivo,
            string carpeta)
        {
            if (archivo == null || archivo.Length == 0)
                throw new Exception("No se recibió ningún archivo.");

            var extension = Path.GetExtension(archivo.FileName);

            var nombreArchivo =
                $"{Guid.NewGuid()}{extension}";

            var ruta = $"{carpeta}/{nombreArchivo}";

            using var memoryStream = new MemoryStream();

            await archivo.CopyToAsync(memoryStream);

            var bytes = memoryStream.ToArray();

            var storage = _supabase.Storage
                .From(_bucket);

            await storage.Upload(
                bytes,
                ruta,
                new Supabase.Storage.FileOptions
                {
                    ContentType = archivo.ContentType,
                    Upsert = false
                });

            var url = storage.GetPublicUrl(ruta);

            return url;
        }

        public async Task EliminarArchivo(string ruta)
        {
            var storage = _supabase.Storage
                .From(_bucket);

            await storage.Remove(new List<string>
            {
                ruta
            });
        }
    }
}