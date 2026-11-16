using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using STAT_Academy.Api.Auth;
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
        private readonly ValidadorSesion _validadorSesion;
        private readonly LoginService _loginService;
        private readonly PasswordHasher<UsuarioModel> _passwordHasher;

        public ContrasenaService(
            ApplicationDbContext context,
            AuditoriaService auditoria,
            ValidadorSesion validadorSesion,
            LoginService loginService)
        {
            _loginService = loginService;
            _context = context;
            _auditoria = auditoria;
            _validadorSesion = validadorSesion;
            _passwordHasher = new PasswordHasher<UsuarioModel>();
        }

        public async Task<string?> GenerarTokenRecuperacionAsync(string email)
        {
            var correoNormalizado = email.Trim().ToLower();

            var usuario = await _context.Usuario.FirstOrDefaultAsync(
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

            await _context.SaveChangesAsync();

            await _auditoria.RegistrarAsync(
                "USUARIO",
                "PASSWORD_RESET_REQUEST",
                $"Se solicitó recuperación de contraseña para {usuario.email}",
                "sistema"
            );

            return token;
        }

        public async Task<bool> RestablecerContrasenaAsync(
            RestablecerContrasenaRequest request)
        {
            if (request.nuevaContrasena != request.confirmarContrasena)
            {
                return false;
            }

            var hashToken = CalcularHashToken(request.token);

            var usuario = await _context.Usuario.FirstOrDefaultAsync(
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
            // Quien demuestra tener el correo recupera el acceso aunque la cuenta estuviera bloqueada.
            await _loginService.DesbloquearAsync(usuario.id);

            await _context.SaveChangesAsync();
            _validadorSesion.Invalidar(usuario.id); // revoca los tokens emitidos con la contraseña anterior

            await _auditoria.RegistrarAsync(
                "USUARIO",
                "PASSWORD_RESET",
                $"La contraseña de {usuario.email} fue restablecida",
                usuario.email ?? "sistema"
            );

            return true;
        }

        /// <summary>
        /// Cambia la contraseña del usuario autenticado.
        /// Devuelve el motivo exacto cuando no se puede cambiar.
        /// </summary>
        public async Task<ResultadoCambioContrasena> CambiarContrasenaAsync(
            int usuarioId,
            CambiarContrasenaRequest request)
        {
            if (request.nuevaContrasena != request.confirmarContrasena)
            {
                return ResultadoCambioContrasena.Error(
                    "La contraseña nueva y su confirmación no coinciden.");
            }

            if (request.nuevaContrasena == request.contrasenaActual)
            {
                return ResultadoCambioContrasena.Error(
                    "La contraseña nueva debe ser diferente de la actual.");
            }

            var usuario = await _context.Usuario.FirstOrDefaultAsync(
                u => u.id == usuarioId && u.estado
            );

            if (usuario == null || string.IsNullOrEmpty(usuario.password))
            {
                return ResultadoCambioContrasena.Error(
                    "No se encontró una cuenta activa para este usuario.");
            }

            var verificacion = _passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.password,
                request.contrasenaActual
            );

            if (verificacion == PasswordVerificationResult.Failed)
            {
                return ResultadoCambioContrasena.Error(
                    "La contraseña actual es incorrecta.");
            }

            usuario.password = _passwordHasher.HashPassword(
                usuario,
                request.nuevaContrasena
            );

            // Un cambio de contraseña invalida cualquier enlace de recuperación pendiente.
            usuario.reset_token_hash = null;
            usuario.reset_token_expiracion = null;
            usuario.reset_token_usado = true;
            usuario.intentos_login = 0;

            await _context.SaveChangesAsync();
            _validadorSesion.Invalidar(usuario.id); // revoca los tokens emitidos con la contraseña anterior

            await _auditoria.RegistrarAsync(
                "USUARIO",
                "PASSWORD_CHANGE",
                $"El usuario {usuario.email} cambió su contraseña",
                usuario.email ?? "sistema",
                usuario.id
            );

            return ResultadoCambioContrasena.Ok(usuario.email, usuario.nombre);
        }

        private static string CalcularHashToken(string token)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = SHA256.HashData(bytes);

            return Convert.ToHexString(hash);
        }
    }
}

namespace STAT_Academy.Api.Services
{
    public record ResultadoCambioContrasena(
        bool exitoso,
        string mensaje,
        string? email = null,
        string? nombre = null)
    {
        public static ResultadoCambioContrasena Ok(string? email, string? nombre) =>
            new(true, "La contraseña fue cambiada correctamente.", email, nombre);

        public static ResultadoCambioContrasena Error(string mensaje) =>
            new(false, mensaje);
    }
}
