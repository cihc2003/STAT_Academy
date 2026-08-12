using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArchivoController : ControllerBase
    {
        private readonly SupabaseStorageService _storageService;

        public ArchivoController(SupabaseStorageService storageService)
        {
            _storageService = storageService;
        }

        // ============================================================
        // SUBIR ARCHIVO
        // POST: api/Archivo/subir
        // ============================================================
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

        // ============================================================
        // OBTENER URL PÚBLICA
        // GET: api/Archivo/url?ruta=archivos/archivo.pdf
        // ============================================================
        [HttpGet("url")]
        public IActionResult ObtenerUrl(
            [FromQuery] string ruta)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ruta))
                {
                    return BadRequest(new
                    {
                        mensaje = "Debe indicar la ruta del archivo."
                    });
                }

                var url = _storageService.ObtenerUrl(ruta);

                return Ok(new
                {
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

        // ============================================================
        // OBTENER URL DE DESCARGA
        // GET: api/Archivo/url-descarga?ruta=archivos/archivo.pdf
        // ============================================================
        [HttpGet("url-descarga")]
        public IActionResult ObtenerUrlDescarga(
            [FromQuery] string ruta)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ruta))
                {
                    return BadRequest(new
                    {
                        mensaje = "Debe indicar la ruta del archivo."
                    });
                }

                var url = _storageService.ObtenerUrlDescarga(ruta);

                return Ok(new
                {
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

        // ============================================================
        // DESCARGAR ARCHIVO
        // GET: api/Archivo/descargar?ruta=archivos/archivo.pdf
        // ============================================================
        [HttpGet("descargar")]
        public async Task<IActionResult> Descargar(
            [FromQuery] string ruta)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ruta))
                {
                    return BadRequest(new
                    {
                        mensaje = "Debe indicar la ruta del archivo."
                    });
                }

                var bytes = await _storageService.DescargarArchivo(ruta);

                var nombreArchivo = Path.GetFileName(ruta);

                var extension = Path.GetExtension(nombreArchivo);

                var contentType = ObtenerContentType(extension);

                return File(
                    bytes,
                    contentType,
                    nombreArchivo);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
        }

        // ============================================================
        // ELIMINAR ARCHIVO
        // DELETE: api/Archivo/eliminar?ruta=archivos/archivo.pdf
        // ============================================================
        [HttpDelete("eliminar")]
        public async Task<IActionResult> Eliminar(
            [FromQuery] string ruta)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ruta))
                {
                    return BadRequest(new
                    {
                        mensaje = "Debe indicar la ruta del archivo."
                    });
                }

                await _storageService.EliminarArchivo(ruta);

                return Ok(new
                {
                    mensaje = "Archivo eliminado correctamente."
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

        // ============================================================
        // DETERMINAR TIPO MIME
        // ============================================================
        private string ObtenerContentType(string extension)
        {
            return extension.ToLower() switch
            {
                ".pdf" => "application/pdf",

                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",

                ".txt" => "text/plain",

                ".doc" => "application/msword",
                ".docx" =>
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

                ".xls" => "application/vnd.ms-excel",
                ".xlsx" =>
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" =>
                    "application/vnd.openxmlformats-officedocument.presentationml.presentation",

                ".zip" => "application/zip",

                _ => "application/octet-stream"
            };
        }
    }
}