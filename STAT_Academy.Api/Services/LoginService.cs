using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Data;
using STAT_Academy.DTOs.Login;
using STAT_Academy.Api.Models;

namespace STAT_Academy.Api.Services
{
    /// <summary>Datos para emitir un token: lo que ve el cliente y el sello de sesión.</summary>
    public record SesionUsuario(LoginResponse Datos, string Sello);

    public enum ResultadoLogin { Correcto, Incorrecto, Bloqueado }

    public class LoginService
    {
        /// <summary>Fallos seguidos que bloquean la cuenta.</summary>
        public const int MaxIntentosFallidos = 5;

        /// <summary>Duración del bloqueo.</summary>
        public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

        private static readonly PasswordHasher<UsuarioModel> PasswordHasher = new();

        // Hash de una contraseña aleatoria: se verifica contra él cuando el correo no existe, para que
        // la respuesta tarde lo mismo y no revele qué correos están registrados.
        private static readonly string HashFicticio =
            PasswordHasher.HashPassword(new UsuarioModel(), Convert.ToBase64String(Guid.NewGuid().ToByteArray()));

        private readonly ApplicationDbContext _context;
        private readonly ValidadorSesion _validadorSesion;
        private readonly ILogger<LoginService> _logger;

        public LoginService(ApplicationDbContext context, ValidadorSesion validadorSesion, ILogger<LoginService> logger)
        {
            _context = context;
            _validadorSesion = validadorSesion;
            _logger = logger;
        }

        public async Task<(ResultadoLogin resultado, SesionUsuario? sesion)> LoginAsync(string email, string password)
        {
            var usuario = await _context.Usuario
                .FirstOrDefaultAsync(u => u.email == email);

            if (usuario == null || !usuario.estado || string.IsNullOrEmpty(usuario.password))
            {
                PasswordHasher.VerifyHashedPassword(new UsuarioModel(), HashFicticio, password);
                return (ResultadoLogin.Incorrecto, null);
            }

            // Bloqueada: ni siquiera se comprueba la contraseña (así no se puede seguir probando).
            if (await BloqueadaHastaAsync(usuario.id) > DateTime.UtcNow)
                return (ResultadoLogin.Bloqueado, null);

            var verificacion = PasswordHasher.VerifyHashedPassword(
                usuario,
                usuario.password,
                password
            );

            if (verificacion == PasswordVerificationResult.Failed)
            {
                usuario.intentos_login += 1;

                var bloquear = usuario.intentos_login >= MaxIntentosFallidos
                               && await BloquearAsync(usuario.id);

                // El contador vuelve a 0 al bloquear: tras el bloqueo hay otros 5 intentos.
                if (bloquear)
                    usuario.intentos_login = 0;

                await _context.SaveChangesAsync();

                return (bloquear ? ResultadoLogin.Bloqueado : ResultadoLogin.Incorrecto, null);
            }

            usuario.intentos_login = 0;
            usuario.ultimo_Login = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (ResultadoLogin.Correcto, Crear(usuario));
        }

        /// <summary>
        /// Sesión del usuario activo con sus datos actuales (p. ej. para emitir un token nuevo
        /// después de cambiar la contraseña, ya que el anterior queda revocado).
        /// </summary>
        public SesionUsuario? SesionDe(int usuarioId)
        {
            var usuario = _context.Usuario.FirstOrDefault(u => u.id == usuarioId && u.estado);
            return usuario == null ? null : Crear(usuario);
        }

        /// <summary>Quita el bloqueo (p. ej. al restablecer la contraseña por correo). Se guarda con SaveChanges del llamador.</summary>
        public void Desbloquear(int usuarioId)
        {
            try
            {
                var bloqueo = _context.UsuarioBloqueo.FirstOrDefault(b => b.fk_Usuario == usuarioId);
                if (bloqueo != null)
                    _context.UsuarioBloqueo.Remove(bloqueo);
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                // Falta aplicar 2026-11-16_usuario_bloqueo.sql: no hay bloqueos que quitar.
            }
        }

        private SesionUsuario Crear(UsuarioModel usuario) => new(
            new LoginResponse
            {
                id = usuario.id,
                email = usuario.email ?? string.Empty,
                nombre = usuario.nombre ?? string.Empty,
                fk_Tipo_Usuario = usuario.fk_Tipo_Usuario
            },
            _validadorSesion.CalcularSello(usuario.password, usuario.fk_Tipo_Usuario));

        private async Task<DateTime?> BloqueadaHastaAsync(int usuarioId)
        {
            try
            {
                return await _context.UsuarioBloqueo
                    .AsNoTracking()
                    .Where(b => b.fk_Usuario == usuarioId)
                    .Select(b => (DateTime?)b.bloqueado_Hasta)
                    .FirstOrDefaultAsync();
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                AvisarTablaFaltante();
                return null;
            }
        }

        /// <summary>Crea o renueva el bloqueo (queda pendiente de SaveChanges). False si la tabla no existe.</summary>
        private async Task<bool> BloquearAsync(int usuarioId)
        {
            try
            {
                var bloqueo = await _context.UsuarioBloqueo.FirstOrDefaultAsync(b => b.fk_Usuario == usuarioId);
                if (bloqueo == null)
                {
                    bloqueo = new UsuarioBloqueoModel { fk_Usuario = usuarioId };
                    _context.UsuarioBloqueo.Add(bloqueo);
                }

                bloqueo.bloqueado_Hasta = DateTime.UtcNow.Add(DuracionBloqueo);
                bloqueo.fecha_Edicion = DateTime.UtcNow;

                _logger.LogWarning("Cuenta {UsuarioId} bloqueada {Minutos} min por intentos fallidos.",
                    usuarioId, DuracionBloqueo.TotalMinutes);
                return true;
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                AvisarTablaFaltante();
                return false;
            }
        }

        private void AvisarTablaFaltante() =>
            _logger.LogWarning("No existe la tabla USUARIO_BLOQUEO (falta 2026-11-16_usuario_bloqueo.sql); solo se aplica el límite en memoria.");
    }
}
