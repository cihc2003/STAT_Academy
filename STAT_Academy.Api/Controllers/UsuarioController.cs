using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsuarioController : Controller
    {
        private readonly UsuarioService _usuarioService;

        public UsuarioController(UsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<UsuarioModel>> GetUsuario()
        {
            return _usuarioService.GetAll();
        }
    }
}