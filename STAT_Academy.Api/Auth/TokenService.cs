using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using STAT_Academy.DTOs.Login;
using System.Security.Claims;

namespace STAT_Academy.Api.Auth
{
    public class TokenService
    {
        private readonly JwtOptions _options;
        private readonly SigningCredentials _credenciales;

        public TokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
            _credenciales = new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(_options.Key)),
                SecurityAlgorithms.HmacSha256);
        }

        public (string token, DateTime expiraEn) GenerarToken(LoginResponse usuario)
        {
            var expiraEn = DateTime.UtcNow.AddMinutes(_options.ExpiracionMinutos);

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                Expires = expiraEn,
                SigningCredentials = _credenciales,
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTiposJwt.UsuarioId, usuario.id.ToString()),
                    new Claim(ClaimTiposJwt.Email, usuario.email),
                    new Claim(ClaimTiposJwt.Nombre, usuario.nombre),
                    new Claim(ClaimTiposJwt.Rol, Roles.DesdeTipoUsuario(usuario.fk_Tipo_Usuario))
                })
            };

            return (new JsonWebTokenHandler().CreateToken(descriptor), expiraEn);
        }
    }

    /// <summary>Nombres cortos de claim dentro del JWT (sin el mapeo heredado de WS-Federation).</summary>
    public static class ClaimTiposJwt
    {
        public const string UsuarioId = JwtRegisteredClaimNames.Sub;
        public const string Email = JwtRegisteredClaimNames.Email;
        public const string Nombre = JwtRegisteredClaimNames.Name;
        public const string Rol = "role";
    }
}
