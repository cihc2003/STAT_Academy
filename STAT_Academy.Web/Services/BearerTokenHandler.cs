using Microsoft.AspNetCore.Authentication;
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

            if (httpContext?.User.Identity?.IsAuthenticated == true
                && request.Headers.Authorization == null)
            {
                var token = await httpContext.GetTokenAsync(NombreToken);

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
