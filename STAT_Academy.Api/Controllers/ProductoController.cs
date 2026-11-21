using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Producto;

namespace STAT_Academy.Api.Controllers
{
    /// <summary>
    /// Tienda: el catálogo y el detalle son públicos (solo productos activos);
    /// crear, editar, imágenes y stock bajo son solo para Admin.
    /// </summary>
    [Authorize(Roles = Roles.Admin)]
    [Route("api/[controller]")]
    public class ProductoController : ApiControllerBase
    {
        private readonly ProductoService _service;

        public ProductoController(ProductoService service)
        {
            _service = service;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync(incluirInactivos: EsAdmin));

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var producto = await _service.GetByIdAsync(id, incluirInactivos: EsAdmin);
            return producto == null ? NotFound(new { message = "El producto no existe o no está disponible." }) : Ok(producto);
        }

        /// <summary>Productos activos en o por debajo de su stock mínimo.</summary>
        [HttpGet("stock-bajo")]
        public async Task<IActionResult> StockBajo() => Ok(await _service.StockBajoAsync());

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] ProductoCreateRequest producto)
        {
            var (resultado, error) = await _service.CrearAsync(producto, UsuarioActualEmail);
            return resultado == null ? BadRequest(new { message = error }) : Ok(resultado);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Editar(int id, [FromBody] ProductoCreateRequest producto)
        {
            var (resultado, error) = await _service.EditarAsync(id, producto, UsuarioActualEmail);

            if (error != null)
                return BadRequest(new { message = error });

            return resultado == null ? NotFound(new { message = "El producto no existe." }) : Ok(resultado);
        }

        [HttpPut("desactivar/{id:int}")]
        public async Task<IActionResult> Desactivar(int id)
        {
            var resultado = await _service.CambiarEstadoAsync(id, false, UsuarioActualEmail);
            return resultado == null ? NotFound(new { message = "El producto no existe." }) : Ok(resultado);
        }

        [HttpPut("activar/{id:int}")]
        public async Task<IActionResult> Activar(int id)
        {
            var resultado = await _service.CambiarEstadoAsync(id, true, UsuarioActualEmail);
            return resultado == null ? NotFound(new { message = "El producto no existe." }) : Ok(resultado);
        }

        // ---------------------------------------------------------------
        // Imágenes
        // ---------------------------------------------------------------

        [HttpPost("{id:int}/imagenes")]
        [RequestSizeLimit(ProductoService.MaxImagenesPorProducto * BlogArchivoValidador.MaxBytesImagen + 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = ProductoService.MaxImagenesPorProducto * BlogArchivoValidador.MaxBytesImagen + 1024 * 1024)]
        public async Task<IActionResult> SubirImagenes(int id, [FromForm] List<IFormFile> imagenes, CancellationToken ct)
        {
            var (producto, errores) = await _service.AgregarImagenesAsync(id, imagenes, UsuarioActualEmail, ct);
            return producto == null
                ? BadRequest(new { message = string.Join(" ", errores), errores })
                : Ok(producto);
        }

        [HttpPatch("{id:int}/imagenes/{imagenId:int}/principal")]
        public async Task<IActionResult> MarcarPrincipal(int id, int imagenId) =>
            await _service.MarcarPrincipalAsync(id, imagenId)
                ? Ok(new { message = "Imagen principal actualizada." })
                : NotFound(new { message = "La imagen no existe." });

        [HttpDelete("{id:int}/imagenes/{imagenId:int}")]
        public async Task<IActionResult> QuitarImagen(int id, int imagenId) =>
            await _service.QuitarImagenAsync(id, imagenId, UsuarioActualEmail)
                ? Ok(new { message = "Imagen eliminada." })
                : NotFound(new { message = "La imagen no existe." });
    }
}
