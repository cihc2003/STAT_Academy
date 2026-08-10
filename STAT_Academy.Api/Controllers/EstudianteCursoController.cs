using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EstudianteCursoController : ControllerBase
    {
        private readonly EstudianteCursoService _service;

        public EstudianteCursoController(EstudianteCursoService service)
        {
            _service = service;
        }

        [HttpGet("{estudianteId}")]
        public IActionResult ObtenerCursos(int estudianteId)
        {
            return Ok(_service.ObtenerCursos(estudianteId));
        }
    }
}