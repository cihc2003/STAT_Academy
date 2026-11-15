using Microsoft.AspNetCore.Identity;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Data;
using STAT_Academy.DTOs.Login;
using STAT_Academy.Api.Models;

namespace STAT_Academy.Api.Services
{
    /// <summary>Datos para emitir un token: lo que ve el cliente y el sello de sesión.</summary>
    public record SesionUsuario(LoginResponse Datos, string Sello);

    public class LoginService
    {
        private readonly ApplicationDbContext _context;
        private readonly ValidadorSesion _validadorSesion;
        private readonly PasswordHasher<UsuarioModel> _passwordHasher;

        public LoginService(ApplicationDbContext context, ValidadorSesion validadorSesion)
        {
            _context = context;
            _validadorSesion = validadorSesion;
            _passwordHasher = new PasswordHasher<UsuarioModel>();
        }

        public SesionUsuario? Login(string email, string password)
        {
            var usuario = _context.Usuario
                .FirstOrDefault(u => u.email == email);

            if (usuario == null || !usuario.estado || string.IsNullOrEmpty(usuario.password))
            {
                return null;
            }

            var resultado = _passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.password,
                password
            );

            if (resultado == PasswordVerificationResult.Failed)
            {
                usuario.intentos_login += 1;
                _context.SaveChanges();

                return null;
            }

            usuario.intentos_login = 0;
            usuario.ultimo_Login = DateTime.UtcNow;

            _context.SaveChanges();

            return Crear(usuario);
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

        private SesionUsuario Crear(UsuarioModel usuario) => new(
            new LoginResponse
            {
                id = usuario.id,
                email = usuario.email ?? string.Empty,
                nombre = usuario.nombre ?? string.Empty,
                fk_Tipo_Usuario = usuario.fk_Tipo_Usuario
            },
            _validadorSesion.CalcularSello(usuario.password, usuario.fk_Tipo_Usuario));
    }
}
