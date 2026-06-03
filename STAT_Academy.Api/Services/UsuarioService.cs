using Microsoft.AspNetCore.Identity;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.DTOs.Usuarios;
using STAT_Academy.Api.Models;

namespace STAT_Academy.Api.Services
{
    public class UsuarioService
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<UsuarioModel> _passwordHasher;

        public UsuarioService(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<UsuarioModel>();
        }

        public List<UsuarioResponse> GetUsuarios()
        {
            return _context.Usuario
                .Select(usuario => new UsuarioResponse
                {
                    id = usuario.id,
                    email = usuario.email,
                    nombre = usuario.nombre,
                    estado = usuario.estado,
                    intentos_login = usuario.intentos_login,
                    fecha_creacion = usuario.fecha_creacion,
                    fecha_edicion = usuario.fecha_edicion,
                    ultimo_Login = usuario.ultimo_Login,
                    fk_Tipo_Usuario = usuario.fk_Tipo_Usuario
                })
                .ToList();
        }

        public UsuarioResponse CreateUsuario(CreateUsuarioRequest request)
        {
            bool emailExiste = _context.Usuario
                .Any(u => u.email == request.email);

            if (emailExiste)
            {
                throw new InvalidOperationException("El correo ya está registrado.");
            }

            var usuario = new UsuarioModel
            {
                email = request.email,
                nombre = request.nombre,
                fk_Tipo_Usuario = request.fk_Tipo_Usuario,
                estado = true,
                intentos_login = 0,
                fecha_creacion = DateTime.UtcNow
            };

            usuario.password = _passwordHasher.HashPassword(usuario, request.password);

            _context.Usuario.Add(usuario);
            _context.SaveChanges();

            return new UsuarioResponse
            {
                id = usuario.id,
                email = usuario.email,
                nombre = usuario.nombre,
                estado = usuario.estado,
                intentos_login = usuario.intentos_login,
                fecha_creacion = usuario.fecha_creacion,
                fecha_edicion = usuario.fecha_edicion,
                ultimo_Login = usuario.ultimo_Login,
                fk_Tipo_Usuario = usuario.fk_Tipo_Usuario
            };
        }
    }
}
