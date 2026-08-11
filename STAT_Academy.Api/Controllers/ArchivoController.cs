using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArchivoController : ControllerBase
    {
        private readonly SupabaseStorageService _storageService;

        public ArchivoController(
            SupabaseStorageService storageService)
        {
            _storageService = storageService;
        }

        [HttpPost("subir")]
        public async Task<IActionResult> Subir(
            IFormFile archivo,
            [FromQuery] string carpeta = "archivos")
        {
            try
            {
                if (archivo == null || archivo.Length == 0)
                {
                    return BadRequest(new
                    {
                        mensaje = "Debe seleccionar un archivo."
                    });
                }

                var url = await _storageService.SubirArchivo(
                    archivo,
                    carpeta);

                return Ok(new
                {
                    mensaje = "Archivo subido correctamente.",
                    url = url
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
        }
    }
}