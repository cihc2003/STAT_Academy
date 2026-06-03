using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;

namespace STAT_Academy.Api.Services
{
    public class LoginService
    {
        private readonly ApplicationDbContext _context;

        public LoginService(ApplicationDbContext context)
        {
            _context = context;
        }

        public UsuarioModel Login(string email, string password)
        {
            var usuario = _context.Usuario.FirstOrDefault(u =>
                u.email == email &&
                u.password == password &&
                u.estado == true);

            if (usuario == null)
                return null;

            usuario.ultimo_Login = DateTime.Now;

            _context.SaveChanges();

            return usuario;
        }
    }
}