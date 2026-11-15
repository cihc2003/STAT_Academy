using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using STAT_Academy.Api.Data;

namespace STAT_Academy.Api.Auth
{
    /// <summary>
    /// Revocación de tokens sin cambiar el esquema: cada JWT lleva un "sello" calculado con el hash de la
    /// contraseña y el tipo de usuario. Si la contraseña cambia, el rol cambia o la cuenta se desactiva,
    /// el sello guardado ya no coincide y la API responde 401 (la Web cierra la sesión).
    ///
    /// El sello esperado se guarda en caché unos segundos para no consultar la base en cada petición;
    /// los servicios que cambian estos datos llaman a <see cref="Invalidar"/> para que el efecto sea inmediato
    /// (en otras instancias de la API tarda como máximo <see cref="DuracionCache"/>).
    /// </summary>
    public class ValidadorSesion
    {
        public const string ClaimSello = "sello";

        private static readonly TimeSpan DuracionCache = TimeSpan.FromSeconds(60);

        private readonly byte[] _clave;
        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory _scopes;

        public ValidadorSesion(IOptions<JwtOptions> options, IMemoryCache cache, IServiceScopeFactory scopes)
        {
            _clave = Convert.FromBase64String(options.Value.Key);
            _cache = cache;
            _scopes = scopes;
        }

        /// <summary>
        /// HMAC con la clave del JWT: el token es legible por el cliente, así que el sello no debe
        /// permitir deducir nada del hash de la contraseña.
        /// </summary>
        public string CalcularSello(string? passwordHash, int fkTipoUsuario)
        {
            var datos = Encoding.UTF8.GetBytes($"{passwordHash}|{fkTipoUsuario}");
            var hmac = HMACSHA256.HashData(_clave, datos);
            return WebEncoders.Base64UrlEncode(hmac, 0, 16);
        }

        /// <summary>Olvida el sello en caché del usuario (llamar tras cambiar contraseña, rol o estado).</summary>
        public void Invalidar(int usuarioId) => _cache.Remove(ClaveCache(usuarioId));

        /// <summary>Evento OnTokenValidated del esquema JWT.</summary>
        public async Task ValidarAsync(TokenValidatedContext contexto)
        {
            var principal = contexto.Principal;
            var sello = principal?.FindFirst(ClaimSello)?.Value;

            if (!int.TryParse(principal?.FindFirst(ClaimTiposJwt.UsuarioId)?.Value, out var usuarioId)
                || string.IsNullOrEmpty(sello))
            {
                contexto.Fail("El token no tiene sello de sesión.");
                return;
            }

            var esperado = await SelloActualAsync(usuarioId, contexto.HttpContext.RequestAborted);

            if (esperado == null
                || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(esperado), Encoding.ASCII.GetBytes(sello)))
            {
                contexto.Fail("La sesión fue revocada.");
            }
        }

        /// <summary>Sello vigente del usuario, o null si no existe o está inactivo.</summary>
        private async Task<string?> SelloActualAsync(int usuarioId, CancellationToken ct)
        {
            if (_cache.TryGetValue(ClaveCache(usuarioId), out string? enCache))
                return enCache;

            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var usuario = await db.Usuario
                .AsNoTracking()
                .Where(u => u.id == usuarioId)
                .Select(u => new { u.estado, u.password, u.fk_Tipo_Usuario })
                .FirstOrDefaultAsync(ct);

            var sello = usuario is { estado: true }
                ? CalcularSello(usuario.password, usuario.fk_Tipo_Usuario)
                : null;

            _cache.Set(ClaveCache(usuarioId), sello, DuracionCache);
            return sello;
        }

        private static string ClaveCache(int usuarioId) => $"sello-sesion:{usuarioId}";
    }
}
