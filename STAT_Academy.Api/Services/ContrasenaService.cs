using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.DTOs.Contrasena;
using System.Security.Cryptography;
using System.Text;

namespace STAT_Academy.Api.Services
{
    public class ContrasenaService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly PasswordHasher<UsuarioModel> _passwordHasher;

        public ContrasenaService(
            ApplicationDbContext context,
            AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
            _passwordHasher = new PasswordHasher<UsuarioModel>();
        }

        public string? GenerarTokenRecuperacion(string email)
        {
            var correoNormalizado = email.Trim().ToLower();

            var usuario = _context.Usuario.FirstOrDefault(
                u => u.email != null
                     && u.email.ToLower() == correoNormalizado
                     && u.estado
            );

            if (usuario == null)
            {
                return null;
            }

            var bytesToken = RandomNumberGenerator.GetBytes(32);
            var token = WebEncoders.Base64UrlEncode(bytesToken);

            usuario.reset_token_hash = CalcularHashToken(token);
            usuario.reset_token_expiracion = DateTime.UtcNow.AddMinutes(30);
            usuario.reset_token_usado = false;
            usuario.fecha_edicion = DateTime.UtcNow;

            _context.SaveChanges();

            _auditoria.Registrar(
                "USUARIO",
                "PASSWORD_RESET_REQUEST",
                $"Se solicitó recuperación de contraseña para {usuario.email}",
                "sistema"
            );

            return token;
        }

        public bool RestablecerContrasena(
            RestablecerContrasenaRequest request)
        {
            if (request.nuevaContrasena != request.confirmarContrasena)
            {
                return false;
            }

            var hashToken = CalcularHashToken(request.token);

            var usuario = _context.Usuario.FirstOrDefault(
                u => u.reset_token_hash == hashToken
                     && !u.reset_token_usado
                     && u.reset_token_expiracion != null
                     && u.reset_token_expiracion > DateTime.UtcNow
                     && u.estado
            );

            if (usuario == null)
            {
                return false;
            }

            usuario.password = _passwordHasher.HashPassword(
                usuario,
                request.nuevaContrasena
            );

            usuario.reset_token_usado = true;
            usuario.reset_token_hash = null;
            usuario.reset_token_expiracion = null;
            usuario.intentos_login = 0;
            usuario.fecha_edicion = DateTime.UtcNow;

            _context.SaveChanges();

            _auditoria.Registrar(
                "USUARIO",
                "PASSWORD_RESET",
                $"La contraseña de {usuario.email} fue restablecida",
                usuario.email ?? "sistema"
            );

            return true;
        }

        public bool CambiarContrasena(
            CambiarContrasenaRequest request)
        {
            if (request.nuevaContrasena != request.confirmarContrasena)
            {
                return false;
            }

            var usuario = _context.Usuario.FirstOrDefault(
                u => u.id == request.usuarioId && u.estado
            );

            if (usuario == null || string.IsNullOrEmpty(usuario.password))
            {
                return false;
            }

            var resultado = _passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.password,
                request.contrasenaActual
            );

            if (resultado == PasswordVerificationResult.Failed)
            {
                return false;
            }

            usuario.password = _passwordHasher.HashPassword(
                usuario,
                request.nuevaContrasena
            );

            usuario.reset_token_hash = null;
            usuario.reset_token_expiracion = null;
            usuario.reset_token_usado = true;
            usuario.intentos_login = 0;
            usuario.fecha_edicion = DateTime.UtcNow;

            _context.SaveChanges();

            _auditoria.Registrar(
                "USUARIO",
                "PASSWORD_CHANGE",
                $"El usuario {usuario.email} cambió su contraseña",
                usuario.email ?? "sistema"
            );

            return true;
        }

        private static string CalcularHashToken(string token)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = SHA256.HashData(bytes);

            return Convert.ToHexString(hash);
        }
    }
}