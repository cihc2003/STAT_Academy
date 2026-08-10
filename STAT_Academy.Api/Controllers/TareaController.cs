
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TareaController : ControllerBase
    {
        private readonly TareaService _service;

        public TareaController(TareaService service)
        {
            _service = service;
        }

        [HttpGet("curso/{cursoId}")]
        public IActionResult ObtenerCurso(int cursoId)
        {
            return Ok(_service.ObtenerPorCurso(cursoId));
        }
    }
}