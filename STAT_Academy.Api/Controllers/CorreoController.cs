using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Correo;

namespace STAT_Academy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CorreoController : ControllerBase
    {
        private readonly CambioCorreoService _cambioCorreoService;
        private readonly CorreoService _correoService;

        public CorreoController(
            CambioCorreoService cambioCorreoService,
            CorreoService correoService)
        {
            _cambioCorreoService = cambioCorreoService;
            _correoService = correoService;
        }

        [HttpPost("solicitar-cambio")]
        public async Task<IActionResult> SolicitarCambio(
            SolicitarCambioCorreoRequest request)
        {
            var resultado = _cambioCorreoService
                .SolicitarCambio(request);

            if (!resultado.exitoso
                || string.IsNullOrWhiteSpace(resultado.token))
            {
                return BadRequest(new
                {
                    message = resultado.mensaje
                });
            }

            await _correoService.EnviarConfirmacionCambioCorreo(
                request.nuevoEmail,
                resultado.token
            );

            return Ok(new
            {
                message =
                    "Se envió un enlace de confirmación al correo nuevo."
            });
        }

        [HttpPost("confirmar-cambio")]
        public IActionResult ConfirmarCambio(
            ConfirmarCambioCorreoRequest request)
        {
            var resultado = _cambioCorreoService
                .ConfirmarCambio(request);

            if (!resultado.exitoso)
            {
                return BadRequest(new
                {
                    message = resultado.mensaje
                });
            }

            return Ok(new
            {
                message = resultado.mensaje
            });
        }
    }
}