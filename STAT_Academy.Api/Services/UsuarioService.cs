using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Auth;
using STAT_Academy.Api.Data;
using STAT_Academy.DTOs.Usuarios;
using STAT_Academy.Api.Models;

namespace STAT_Academy.Api.Services
{
    public class UsuarioService
    {
        private const string Entidad = "USUARIO";
        private const int TipoAdmin = 1;

        public const int TamanoPaginaMaximo = 100;

        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly PasswordHasher<UsuarioModel> _passwordHasher;

        public UsuarioService(ApplicationDbContext context, AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
            _passwordHasher = new PasswordHasher<UsuarioModel>();
        }

        // ==================================================================
        // Consultas
        // ==================================================================

        public List<UsuarioResponse> GetUsuarios()
        {
            return ConFechas(_context.Usuario.AsNoTracking())
                .AsEnumerable()
                .Select(Map)
                .ToList();
        }

        public UsuarioResponse? GetUsuarioById(int id)
        {
            var fila = ConFechas(_context.Usuario.AsNoTracking().Where(u => u.id == id)).FirstOrDefault();
            return fila == null ? null : Map(fila);
        }

        /// <summary>
        /// Lista paginada para el panel de administración. Filtra y ordena en la base de datos.
        /// </summary>
        /// <param name="texto">Busca en el nombre y en el correo.</param>
        /// <param name="orden">nombre, nombre_desc, registro, registro_desc, login o login_desc.</param>
        public UsuarioPaginaResponse Buscar(string? texto, int? tipo, bool? estado, string? orden, int pagina, int tamano)
        {
            tamano = Math.Clamp(tamano, 1, TamanoPaginaMaximo);
            pagina = Math.Max(pagina, 1);

            var consulta = _context.Usuario.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(texto))
            {
                var t = texto.Trim();
                consulta = consulta.Where(u =>
                    (u.nombre != null && u.nombre.Contains(t)) ||
                    (u.email != null && u.email.Contains(t)));
            }

            if (tipo != null)
                consulta = consulta.Where(u => u.fk_Tipo_Usuario == tipo);

            if (estado != null)
                consulta = consulta.Where(u => u.estado == estado);

            var total = consulta.Count();
            var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamano));
            pagina = Math.Min(pagina, totalPaginas);

            var conFechas = ConFechas(consulta);

            conFechas = orden switch
            {
                "nombre_desc" => conFechas.OrderByDescending(f => f.nombre).ThenBy(f => f.id),
                "registro" => conFechas.OrderBy(f => f.fechaRegistro == null).ThenBy(f => f.fechaRegistro).ThenBy(f => f.id),
                "registro_desc" => conFechas.OrderBy(f => f.fechaRegistro == null).ThenByDescending(f => f.fechaRegistro).ThenBy(f => f.id),
                "login" => conFechas.OrderBy(f => f.ultimoLogin == null).ThenBy(f => f.ultimoLogin).ThenBy(f => f.id),
                "login_desc" => conFechas.OrderBy(f => f.ultimoLogin == null).ThenByDescending(f => f.ultimoLogin).ThenBy(f => f.id),
                _ => conFechas.OrderBy(f => f.nombre).ThenBy(f => f.id)
            };

            var items = conFechas
                .Skip((pagina - 1) * tamano)
                .Take(tamano)
                .AsEnumerable()
                .Select(Map)
                .ToList();

            return new UsuarioPaginaResponse
            {
                items = items,
                pagina = pagina,
                tamano = tamano,
                total = total,
                totalPaginas = totalPaginas,
                resumen = Resumen()
            };
        }

        public List<UsuarioResponse> FiltrarPorTipo(int tipo)
        {
            // Nunca devolver la entidad: incluye el hash de la contraseña y tokens.
            return ConFechas(_context.Usuario.AsNoTracking().Where(u => u.fk_Tipo_Usuario == tipo))
                .AsEnumerable()
                .Select(Map)
                .ToList();
        }

        public List<UsuarioResponse> UsuariosActivos()
        {
            return ConFechas(_context.Usuario.AsNoTracking().Where(u => u.estado))
                .AsEnumerable()
                .Select(Map)
                .ToList();
        }

        // ==================================================================
        // Altas
        // ==================================================================

        /// <summary>Alta hecha por un Admin: respeta el tipo elegido.</summary>
        public UsuarioResponse CreateUsuario(CreateUsuarioRequest request, string actorEmail)
        {
            var email = request.email.Trim();

            if (_context.Usuario.Any(u => u.email == email))
                throw new InvalidOperationException("El correo ya está registrado.");

            var usuario = new UsuarioModel
            {
                email = email,
                nombre = request.nombre.Trim(),
                fk_Tipo_Usuario = request.fk_Tipo_Usuario,
                estado = true,
                intentos_login = 0,
            };

            usuario.password = _passwordHasher.HashPassword(usuario, request.password);

            _context.Usuario.Add(usuario);
            _context.SaveChanges();

            // La fecha de registro sale de este registro de auditoría.
            _auditoria.Registrar(Entidad, "CREATE", $"Usuario {usuario.email} creado", actorEmail, usuario.id);

            return GetUsuarioById(usuario.id)!;
        }

        /// <summary>Registro público: siempre crea un Cliente.</summary>
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
            };

            usuario.password = _passwordHasher.HashPassword(usuario, request.password);

            _context.Usuario.Add(usuario);
            _context.SaveChanges();

            _auditoria.Registrar(Entidad, "CREATE", $"Usuario {usuario.email} registrado", "sistema", usuario.id);

            return GetUsuarioById(usuario.id)!;
        }

        // ==================================================================
        // Cambios (todos quedan en AUDITORIA)
        // ==================================================================

        /// <summary>Activa o desactiva. Un Admin no puede desactivarse a sí mismo.</summary>
        /// <exception cref="InvalidOperationException">Si el Admin intenta desactivarse.</exception>
        public UsuarioResponse? CambiarEstado(int id, bool activo, int actorId, string actorEmail)
        {
            var usuario = _context.Usuario.FirstOrDefault(u => u.id == id);

            if (usuario == null)
                return null;

            if (!activo && id == actorId)
                throw new InvalidOperationException("No puede desactivar su propia cuenta.");

            if (usuario.estado != activo)
            {
                usuario.estado = activo;
                _context.SaveChanges();

                _auditoria.Registrar(Entidad, activo ? "ENABLE" : "DISABLE",
                    $"Usuario {usuario.email} {(activo ? "activado" : "desactivado")}", actorEmail, usuario.id);
            }

            return GetUsuarioById(id);
        }

        /// <summary>
        /// Edita nombre, tipo y (opcionalmente) estado. El correo no se modifica aquí.
        /// Un Admin no puede quitarse el rol de administrador ni desactivarse a sí mismo.
        /// </summary>
        public UsuarioResponse? UpdateUsuario(int id, UpdateUsuarioRequest request, int actorId, string actorEmail)
        {
            var usuario = _context.Usuario.FirstOrDefault(u => u.id == id);

            if (usuario == null)
                return null;

            if (id == actorId && request.fk_Tipo_Usuario != TipoAdmin)
                throw new InvalidOperationException("No puede quitarse el rol de administrador a sí mismo.");

            if (id == actorId && request.estado == false)
                throw new InvalidOperationException("No puede desactivar su propia cuenta.");

            var cambios = new List<string>();
            var nombre = request.nombre.Trim();

            if (usuario.nombre != nombre)
                cambios.Add("nombre");

            if (usuario.fk_Tipo_Usuario != request.fk_Tipo_Usuario)
                cambios.Add($"tipo {Roles.DesdeTipoUsuario(usuario.fk_Tipo_Usuario)} → {Roles.DesdeTipoUsuario(request.fk_Tipo_Usuario)}");

            if (request.estado != null && usuario.estado != request.estado)
                cambios.Add(request.estado.Value ? "activado" : "desactivado");

            if (cambios.Count > 0)
            {
                usuario.nombre = nombre;
                usuario.fk_Tipo_Usuario = request.fk_Tipo_Usuario;
                if (request.estado != null)
                    usuario.estado = request.estado.Value;

                _context.SaveChanges();

                var detalle = Recortar($"Usuario {usuario.email} actualizado ({string.Join(", ", cambios)})");
                _auditoria.Registrar(Entidad, "UPDATE", detalle, actorEmail, usuario.id);
            }

            return GetUsuarioById(id);
        }

        // ==================================================================
        // Ayudantes
        // ==================================================================

        private sealed class UsuarioConFechas
        {
            public int id { get; set; }
            public string? email { get; set; }
            public string? nombre { get; set; }
            public bool estado { get; set; }
            public int intentosLogin { get; set; }
            public DateTime? ultimoLogin { get; set; }
            public int tipo { get; set; }
            public DateTime? fechaRegistro { get; set; }
            public DateTime? fechaEdicion { get; set; }
        }

        /// <summary>
        /// Agrega la fecha de registro y de la última modificación tomadas de AUDITORIA.
        /// Los registros nuevos traen ENTIDAD_ID; los antiguos solo el correo en la descripción,
        /// por eso se comparan también las descripciones exactas que se usaban antes.
        /// </summary>
        private IQueryable<UsuarioConFechas> ConFechas(IQueryable<UsuarioModel> usuarios) =>
            usuarios.Select(u => new UsuarioConFechas
            {
                id = u.id,
                email = u.email,
                nombre = u.nombre,
                estado = u.estado,
                intentosLogin = u.intentos_login,
                ultimoLogin = u.ultimo_Login,
                tipo = u.fk_Tipo_Usuario,

                fechaRegistro = _context.Auditoria
                    .Where(a => a.entidad == Entidad && a.accion == "CREATE" &&
                                (a.entidad_id == u.id ||
                                 (a.entidad_id == null &&
                                  (a.descripcion == "Usuario " + u.email + " creado" ||
                                   a.descripcion == "Usuario " + u.email + " registrado"))))
                    .Min(a => (DateTime?)a.fecha),

                fechaEdicion = _context.Auditoria
                    .Where(a => a.entidad == Entidad && a.accion != "CREATE" &&
                                (a.entidad_id == u.id ||
                                 (a.entidad_id == null &&
                                  (a.descripcion == "Usuario " + u.email + " actualizado" ||
                                   a.descripcion == "Usuario " + u.email + " activado" ||
                                   a.descripcion == "Usuario " + u.email + " desactivado"))))
                    .Max(a => (DateTime?)a.fecha)
            });

        private UsuarioResumenResponse Resumen()
        {
            var porTipo = _context.Usuario
                .GroupBy(u => u.fk_Tipo_Usuario)
                .Select(g => new { tipo = g.Key, cantidad = g.Count(), inactivos = g.Count(u => !u.estado) })
                .ToList();

            return new UsuarioResumenResponse
            {
                total = porTipo.Sum(g => g.cantidad),
                admins = porTipo.Where(g => g.tipo == 1).Sum(g => g.cantidad),
                tutores = porTipo.Where(g => g.tipo == 2).Sum(g => g.cantidad),
                clientes = porTipo.Where(g => g.tipo == 3).Sum(g => g.cantidad),
                inactivos = porTipo.Sum(g => g.inactivos)
            };
        }

        private static UsuarioResponse Map(UsuarioConFechas f) => new()
        {
            id = f.id,
            email = f.email ?? "",
            nombre = f.nombre ?? "",
            estado = f.estado,
            intentos_login = f.intentosLogin,
            ultimo_Login = Utc(f.ultimoLogin),
            fk_Tipo_Usuario = f.tipo,
            tipo = Roles.DesdeTipoUsuario(f.tipo),
            fecha_Registro = Utc(f.fechaRegistro),
            fecha_Edicion = Utc(f.fechaEdicion)
        };

        /// <summary>Las fechas se guardan en UTC: se marcan así para que el JSON lleve la "Z".</summary>
        private static DateTime? Utc(DateTime? fecha) =>
            fecha == null ? null : DateTime.SpecifyKind(fecha.Value, DateTimeKind.Utc);

        private static string Recortar(string texto) =>
            texto.Length > 255 ? texto[..254] + "…" : texto;
    }
}
