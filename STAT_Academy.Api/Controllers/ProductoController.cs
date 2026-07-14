using Microsoft.AspNetCore.Mvc;
using STAT_Academy.DTOs.Producto;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductoController : ControllerBase
    {
        private readonly ProductoService _service;

        public ProductoController(ProductoService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_service.GetAll());
        }

        [HttpPost]
        public IActionResult Crear([FromBody] ProductoCreateRequest producto)
        {
            var result = _service.Crear(producto);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public IActionResult Editar(int id, [FromBody] ProductoCreateRequest producto)
        {
            var result = _service.Editar(id, producto);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPut("desactivar/{id}")]
        public IActionResult Desactivar(int id)
        {
            return Ok(_service.Desactivar(id));
        }
    }
}