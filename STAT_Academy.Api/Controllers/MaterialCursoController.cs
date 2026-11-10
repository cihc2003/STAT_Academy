using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaterialCursoController : ControllerBase
    {
        private readonly MaterialCursoService _service;

        public MaterialCursoController(MaterialCursoService service)
        {
            _service = service;
        }

        [HttpGet("curso/{cursoId}")]
        public IActionResult ObtenerPorCurso(int cursoId)
        {
            return Ok(_service.ObtenerPorCurso(cursoId));
        }
    }
}