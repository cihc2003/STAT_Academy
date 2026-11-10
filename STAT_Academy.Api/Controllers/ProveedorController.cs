using Microsoft.AspNetCore.Mvc;
using STAT_Academy.DTOs.Proveedor;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProveedorController : ControllerBase
    {
        private readonly ProveedorService _service;

        public ProveedorController(ProveedorService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_service.GetAll());
        }

        [HttpPost]
        public IActionResult Crear([FromBody] ProveedorCreateRequest proveedor)
        {
            var result = _service.Crear(proveedor);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public IActionResult Editar(int id, [FromBody] ProveedorCreateRequest proveedor)
        {
            var result = _service.Editar(id, proveedor);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
        [HttpPatch("desactivar/{id}")]
        public IActionResult Desactivar(int id)
        {
            try
            {
                var result = _service.Desactivar(id);

                if (result == null)
                    return NotFound($"Proveedor con id {id} no existe");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var proveedor = _service.GetById(id);

            if (proveedor == null)
                return NotFound();

            return Ok(proveedor);
        }

    }
}
