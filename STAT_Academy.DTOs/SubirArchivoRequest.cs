using Microsoft.AspNetCore.Http;

namespace STAT_Academy.Api.DTOs
{
    public class SubirArchivoRequest
    {
        public IFormFile Archivo { get; set; } = null!;
        public bool EsPrincipal { get; set; }
    }
}