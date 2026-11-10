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

        /// <summary>True si el usuario autenticado es el dueño del recurso o es Admin.</summary>
        protected bool PuedeActuarSobre(int usuarioId) =>
            EsAdmin || usuarioId == UsuarioActualId;
    }
}
