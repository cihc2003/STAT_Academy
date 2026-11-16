using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Auth;
using STAT_Academy.DTOs.Proveedor;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    [Route("api/[controller]")]
    public class ProveedorController : ControllerBase
    {
        private readonly ProveedorService _service;

        public ProveedorController(ProveedorService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] ProveedorCreateRequest proveedor)
        {
            var result = await _service.CrearAsync(proveedor);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(int id, [FromBody] ProveedorCreateRequest proveedor)
        {
            var result = await _service.EditarAsync(id, proveedor);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
        [HttpPatch("desactivar/{id}")]
        public async Task<IActionResult> Desactivar(int id)
        {
            try
            {
                var result = await _service.DesactivarAsync(id);

                if (result == null)
                    return NotFound("El proveedor no existe.");

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                // Regla de negocio (tiene productos activos); otros errores van al manejador global.
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var proveedor = await _service.GetByIdAsync(id);

            if (proveedor == null)
                return NotFound();

            return Ok(proveedor);
        }

    }
}
