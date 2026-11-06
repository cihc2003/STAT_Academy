using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.DTOs;
using STAT_Academy.Api.Services;
using STAT_Academy.Api.Storage;
using STAT_Academy.DTOs;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("api/productos")]
    public class ProductoImagenesController : ControllerBase
    {
        private readonly IProductoImagenService _servicio;

        public ProductoImagenesController(IProductoImagenService servicio)
        {
            _servicio = servicio;
        }

        /// <summary>Lista las imágenes activas de un producto (la principal primero).</summary>
        // GET api/productos/15/imagenes
        [HttpGet("{productoId:int}/imagenes")]
        public async Task<ActionResult<List<ArchivoDto>>> Listar(int productoId, CancellationToken ct)
        {
            var imagenes = await _servicio.ListarAsync(productoId, ct);
            return Ok(imagenes);
        }

        /// <summary>Sube una imagen al producto.</summary>
        // POST api/productos/15/imagenes   (multipart/form-data: Archivo, EsPrincipal)
        [HttpPost("{productoId:int}/imagenes")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)] // 5 MB del archivo + margen del formulario
        public async Task<ActionResult<ArchivoDto>> Subir(int productoId, [FromForm] SubirArchivoRequest request, CancellationToken ct)
        {
            try
            {
                var dto = await _servicio.SubirAsync(productoId, request.Archivo, request.EsPrincipal, ct);

                if (dto is null)
                    return NotFound(new { mensaje = "Producto no encontrado o inactivo." });

                return Ok(dto);
            }
            catch (ArchivoInvalidoException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>Marca una imagen como principal (desmarca las demás del mismo producto).</summary>
        // PUT api/productos/imagenes/42/principal
        [HttpPut("imagenes/{imagenId:int}/principal")]
        public async Task<IActionResult> MarcarPrincipal(int imagenId, CancellationToken ct)
        {
            var ok = await _servicio.MarcarPrincipalAsync(imagenId, ct);
            return ok ? NoContent() : NotFound(new { mensaje = "Imagen no encontrada." });
        }

        /// <summary>Elimina una imagen (estado = 0 y borra el archivo de Azure).</summary>
        // DELETE api/productos/imagenes/42
        [HttpDelete("imagenes/{imagenId:int}")]
        public async Task<IActionResult> Eliminar(int imagenId, CancellationToken ct)
        {
            var ok = await _servicio.EliminarAsync(imagenId, ct);
            return ok ? NoContent() : NotFound(new { mensaje = "Imagen no encontrada." });
        }
    }
}