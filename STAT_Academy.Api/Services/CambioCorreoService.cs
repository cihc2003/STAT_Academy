using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.DTOs.Correo;
using System.Security.Cryptography;
using System.Text;

namespace STAT_Academy.Api.Services
{
    public class CambioCorreoService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly PasswordHasher<UsuarioModel> _passwordHasher;

        public CambioCorreoService(
            ApplicationDbContext context,
            AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
            _passwordHasher = new PasswordHasher<UsuarioModel>();
        }

        public (
            bool exitoso,
            string? token,
            string mensaje
        ) SolicitarCambio(SolicitarCambioCorreoRequest request)
        {
            var usuario = _context.Usuario.FirstOrDefault(
                u => u.id == request.usuarioId && u.estado
            );

            if (usuario == null || string.IsNullOrEmpty(usuario.password))
            {
                return (
                    false,
                    null,
                    "Usuario no encontrado."
                );
            }

            var verificacion = _passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.password,
                request.contrasenaActual
            );

            if (verificacion == PasswordVerificationResult.Failed)
            {
                return (
                    false,
                    null,
                    "La contraseña actual es incorrecta."
                );
            }

            var correoNuevo = request.nuevoEmail
                .Trim()
                .ToLower();

            var correoActual = usuario.email?
                .Trim()
                .ToLower();

            if (correoNuevo == correoActual)
            {
                return (
                    false,
                    null,
                    "El correo nuevo debe ser diferente al correo actual."
                );
            }

            var correoEnUso = _context.Usuario.Any(
                u => u.email != null
                     && u.email.ToLower() == correoNuevo
            );

            if (correoEnUso)
            {
                return (
                    false,
                    null,
                    "El correo nuevo ya está registrado."
                );
            }

            var bytesToken = RandomNumberGenerator.GetBytes(32);
            var token = WebEncoders.Base64UrlEncode(bytesToken);

            usuario.nuevo_email_pendiente = correoNuevo;
            usuario.email_change_token_hash =
                CalcularHashToken(token);
            usuario.email_change_token_expiracion =
                DateTime.UtcNow.AddMinutes(30);
            usuario.fecha_edicion = DateTime.UtcNow;

            _context.SaveChanges();

            _auditoria.Registrar(
                "USUARIO",
                "EMAIL_CHANGE_REQUEST",
                $"Se solicitó cambiar el correo de " +
                $"{usuario.email} a {correoNuevo}",
                usuario.email ?? "sistema",
                usuario.id
            );

            return (
                true,
                token,
                "Solicitud de cambio de correo creada."
            );
        }

        public (
            bool exitoso,
            string mensaje
        ) ConfirmarCambio(ConfirmarCambioCorreoRequest request)
        {
            var hashToken = CalcularHashToken(request.token);

            var usuario = _context.Usuario.FirstOrDefault(
                u => u.email_change_token_hash == hashToken
                     && u.email_change_token_expiracion != null
                     && u.email_change_token_expiracion > DateTime.UtcNow
                     && u.nuevo_email_pendiente != null
                     && u.estado
            );

            if (usuario == null)
            {
                return (
                    false,
                    "El enlace es inválido o expiró."
                );
            }

            var correoNuevo = usuario.nuevo_email_pendiente;

            var correoEnUso = _context.Usuario.Any(
                u => u.id != usuario.id
                     && u.email != null
                     && u.email.ToLower() == correoNuevo.ToLower()
            );

            if (correoEnUso)
            {
                return (
                    false,
                    "El correo ya está registrado por otro usuario."
                );
            }

            var correoAnterior = usuario.email;

            usuario.email = correoNuevo;
            usuario.nuevo_email_pendiente = null;
            usuario.email_change_token_hash = null;
            usuario.email_change_token_expiracion = null;
            usuario.fecha_edicion = DateTime.UtcNow;

            _context.SaveChanges();

            _auditoria.Registrar(
                "USUARIO",
                "EMAIL_CHANGE",
                $"El correo cambió de {correoAnterior} " +
                $"a {usuario.email}",
                usuario.email ?? "sistema",
                usuario.id
            );

            return (
                true,
                "El correo fue cambiado correctamente."
            );
        }

        private static string CalcularHashToken(string token)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = SHA256.HashData(bytes);

            return Convert.ToHexString(hash);
        }
    }
}