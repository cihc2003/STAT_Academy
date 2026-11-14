using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Auth;

namespace STAT_Academy.Api.Controllers
{
    /// <summary>
    /// Base para controladores que necesitan saber quién hace la petición.
    /// La identidad sale SIEMPRE del token, nunca de un id enviado por el cliente.
    /// </summary>
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected int UsuarioActualId =>
            int.TryParse(User.FindFirst(ClaimTiposJwt.UsuarioId)?.Value, out var id)
                ? id
                : throw new InvalidOperationException("El token no contiene el id del usuario.");

        protected string UsuarioActualEmail =>
            User.FindFirst(ClaimTiposJwt.Email)?.Value ?? "desconocido";

        protected bool EsAdmin => User.IsInRole(Roles.Admin);

        /// <summary>
        /// True si el usuario autenticado tiene rol Tutor y es el tutor indicado.
        /// Exige el rol para que un dato mal cargado (un estudiante como FK_Tutor) no dé permisos de tutor.
        /// </summary>
        protected bool EsTutor(int tutorId) =>
            User.Identity?.IsAuthenticated == true && User.IsInRole(Roles.Tutor) && tutorId == UsuarioActualId;

        /// <summary>True si el usuario autenticado es el dueño del recurso o es Admin.</summary>
        protected bool PuedeActuarSobre(int usuarioId) =>
            EsAdmin || usuarioId == UsuarioActualId;
    }
}
