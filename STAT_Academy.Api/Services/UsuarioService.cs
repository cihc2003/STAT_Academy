using Microsoft.AspNetCore.Identity;
using STAT_Academy.Api.Data;
using STAT_Academy.DTOs.Usuarios;
using STAT_Academy.Api.Models;

namespace STAT_Academy.Api.Services
{
    public class UsuarioService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly PasswordHasher<UsuarioModel> _passwordHasher;

        public UsuarioService(ApplicationDbContext context, AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
            _passwordHasher = new PasswordHasher<UsuarioModel>();
        }

        public List<UsuarioResponse> GetUsuarios()
        {
            return _context.Usuario
                .Select(usuario => MapToUsuarioResponse(usuario))
                .ToList();
        }

        public UsuarioResponse? GetUsuarioById(int id)
        {
            var usuario = _context.Usuario.FirstOrDefault(u => u.id == id);

            if (usuario == null)
                return null;

            return MapToUsuarioResponse(usuario);
        }

        public UsuarioResponse CreateUsuario(CreateUsuarioRequest request)
        {
            bool emailExiste = _context.Usuario.Any(u => u.email == request.email);

            if (emailExiste)
                throw new InvalidOperationException("El correo ya está registrado.");

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

            _auditoria.Registrar(
                "USUARIO",
                "CREATE",
                $"Usuario {usuario.email} creado",
                "admin"
            );

            return MapToUsuarioResponse(usuario);
        }

        public UsuarioResponse RegisterUsuario(RegisterUsuarioRequest request)
        {
            bool emailExiste = _context.Usuario.Any(u => u.email == request.email);

            if (emailExiste)
                throw new InvalidOperationException("El correo ya está registrado.");

            var usuario = new UsuarioModel
            {
                email = request.email,
                nombre = request.nombre,
                fk_Tipo_Usuario = 3,
                estado = true,
                intentos_login = 0,
                fecha_creacion = DateTime.UtcNow
            };

            usuario.password = _passwordHasher.HashPassword(usuario, request.password);

            _context.Usuario.Add(usuario);
            _context.SaveChanges();

            _auditoria.Registrar(
                "USUARIO",
                "CREATE",
                $"Usuario {usuario.email} registrado",
                "sistema"
            );

            return MapToUsuarioResponse(usuario);
        }

        public UsuarioResponse? DesactivarUsuario(int id)
        {
            var usuario = _context.Usuario.FirstOrDefault(u => u.id == id);

            if (usuario == null)
                return null;

            usuario.estado = false;
            usuario.fecha_edicion = DateTime.UtcNow;

            _context.SaveChanges();

            _auditoria.Registrar(
                "USUARIO",
                "DELETE",
                $"Usuario {usuario.email} desactivado",
                "admin"
            );

            return MapToUsuarioResponse(usuario);
        }

        public UsuarioResponse? UpdateUsuario(int id, UpdateUsuarioRequest request)
        {
            var usuario = _context.Usuario.FirstOrDefault(u => u.id == id);

            if (usuario == null)
                return null;

            bool emailExiste = _context.Usuario.Any(u => u.email == request.email && u.id != id);

            if (emailExiste)
                throw new InvalidOperationException("El correo ya está registrado.");

            usuario.email = request.email;
            usuario.nombre = request.nombre;
            usuario.fk_Tipo_Usuario = request.fk_Tipo_Usuario;
            usuario.estado = request.estado;
            usuario.fecha_edicion = DateTime.UtcNow;

            _context.SaveChanges();

            _auditoria.Registrar(
                "USUARIO",
                "UPDATE",
                $"Usuario {usuario.email} actualizado",
                "admin"
            );

            return MapToUsuarioResponse(usuario);
        }

        public List<UsuarioModel> FiltrarPorTipo(int tipo)
        {
            return _context.Usuario.Where(u => u.fk_Tipo_Usuario == tipo).ToList();
        }

        public List<UsuarioModel> UsuariosActivos()
        {
            return _context.Usuario.Where(u => u.estado == true).ToList();
        }

        private static UsuarioResponse MapToUsuarioResponse(UsuarioModel usuario)
        {
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