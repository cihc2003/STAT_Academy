using Microsoft.AspNetCore.Identity;
using STAT_Academy.Api.Data;
using STAT_Academy.DTOs.Login;
using STAT_Academy.Api.Models;

namespace STAT_Academy.Api.Services
{
    public class LoginService
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<UsuarioModel> _passwordHasher;

        public LoginService(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<UsuarioModel>();
        }

        public LoginResponse? Login(string email, string password)
        {
            var usuario = _context.Usuario
                .FirstOrDefault(u => u.email == email);

            if (usuario == null)
            {
                return null;
            }

            if (!usuario.estado)
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

            return new LoginResponse
            {
                id = usuario.id,
                email = usuario.email,
                nombre = usuario.nombre,
                fk_Tipo_Usuario = usuario.fk_Tipo_Usuario
            };
        }
    }
}