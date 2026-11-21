using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace STAT_Academy.Web.Services
{
    /// <summary>Crea o vuelve a emitir la cookie de sesión que guarda el JWT de la API.</summary>
    public static class SesionWeb
    {
        private const string NombreExpira = "expires_at";

        /// <summary>Crea la cookie de sesión con el JWT de la API (cifrado dentro de la cookie).</summary>
        public static async Task IniciarAsync(HttpContext http, ClaimsPrincipal principal, string token, DateTime expiraEnUtc)
        {
            var expiraToken = new DateTimeOffset(DateTime.SpecifyKind(expiraEnUtc, DateTimeKind.Utc));
            await IniciarAsync(http, principal, token, expiraToken);
        }

        /// <summary>
        /// Vuelve a emitir la cookie con el mismo token y otro nombre visible (tras editar el perfil).
        /// Devuelve false si la sesión no tiene token (hay que iniciar sesión de nuevo).
        /// </summary>
        public static async Task<bool> ActualizarNombreAsync(HttpContext http, string nombre)
        {
            var token = await http.GetTokenAsync(BearerTokenHandler.NombreToken);
            var expiraTexto = await http.GetTokenAsync(NombreExpira);

            if (string.IsNullOrEmpty(token) || !DateTimeOffset.TryParse(expiraTexto, out var expira))
                return false;

            var claims = http.User.Claims.Where(c => c.Type != ClaimTypes.Name).Append(new Claim(ClaimTypes.Name, nombre));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

            await IniciarAsync(http, principal, token, expira);
            return true;
        }

        private static async Task IniciarAsync(HttpContext http, ClaimsPrincipal principal, string token, DateTimeOffset expiraToken)
        {
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false,
                // La sesión nunca dura más que el token de la API.
                ExpiresUtc = expiraToken < DateTimeOffset.UtcNow.AddMinutes(30)
                    ? expiraToken
                    : DateTimeOffset.UtcNow.AddMinutes(30)
            };

            // BearerTokenHandler lo reenvía a la API en cada petición.
            authProperties.StoreTokens(new[]
            {
                new AuthenticationToken { Name = BearerTokenHandler.NombreToken, Value = token },
                new AuthenticationToken { Name = NombreExpira, Value = expiraToken.ToString("o") }
            });

            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        }
    }
}
