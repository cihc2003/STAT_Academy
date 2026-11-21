using Microsoft.AspNetCore.Authentication;
using System.Net;
using System.Net.Http.Headers;

namespace STAT_Academy.Web.Services
{
    /// <summary>
    /// Adjunta el JWT del usuario (guardado en la cookie de autenticación al hacer login)
    /// a cada petición que la Web hace a la API.
    /// </summary>
    public class BearerTokenHandler : DelegatingHandler
    {
        public const string NombreToken = "access_token";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public BearerTokenHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var tokenAdjunto = false;

            if (httpContext?.User.Identity?.IsAuthenticated == true
                && request.Headers.Authorization == null)
            {
                var token = await httpContext.GetTokenAsync(NombreToken);

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                    tokenAdjunto = true;
                }
            }

            var response = await base.SendAsync(request, cancellationToken);

            // 401 del middleware JWT (trae "WWW-Authenticate: Bearer"): el token venció o no es válido.
            // Un 401 de negocio (p. ej. credenciales incorrectas en el login) no trae ese encabezado.
            if (tokenAdjunto
                && response.StatusCode == HttpStatusCode.Unauthorized
                && response.Headers.WwwAuthenticate.Any(h => h.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase)))
            {
                response.Dispose();
                throw new ApiSesionExpiradaException();
            }

            return response;
        }
    }
}
