using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Producto;

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

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var proveedor = _service.GetById(id);

            if (proveedor == null)
                return NotFound();

            return Ok(proveedor);
        }
        [HttpGet("activos")]
        public IActionResult GetActivos()
        {
            return Ok(_service.GetActivos());
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
        [HttpPut("activar/{id}")]
        public IActionResult Activar(int id)
        {
            var producto = _service.Activar(id);

            if (producto == null)
                return NotFound();

            return Ok(producto);
        }

    }
}