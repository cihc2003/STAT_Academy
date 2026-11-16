using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
        private readonly ValidadorSesion _validadorSesion;
        private readonly PasswordHasher<UsuarioModel> _passwordHasher;
        private readonly ILogger<UsuarioService> _logger;

        public UsuarioService(ApplicationDbContext context, AuditoriaService auditoria, ValidadorSesion validadorSesion,
            ILogger<UsuarioService>? logger = null)
        {
            _context = context;
            _auditoria = auditoria;
            _validadorSesion = validadorSesion;
            _logger = logger ?? NullLogger<UsuarioService>.Instance;
            _passwordHasher = new PasswordHasher<UsuarioModel>();
        }

        // ==================================================================
        // Consultas
        // ==================================================================

        public async Task<List<UsuarioResponse>> GetUsuariosAsync() =>
            (await ConFechas(_context.Usuario.AsNoTracking()).ToListAsync())
                .Select(Map)
                .ToList();

        public async Task<UsuarioResponse?> GetUsuarioByIdAsync(int id)
        {
            var fila = await ConFechas(_context.Usuario.AsNoTracking().Where(u => u.id == id)).FirstOrDefaultAsync();
            return fila == null ? null : Map(fila);
        }

        /// <summary>
        /// Lista paginada para el panel de administración. Filtra y ordena en la base de datos.
        /// </summary>
        /// <param name="texto">Busca en el nombre y en el correo.</param>
        /// <param name="orden">nombre, nombre_desc, registro, registro_desc, login o login_desc.</param>
        public async Task<UsuarioPaginaResponse> BuscarAsync(string? texto, int? tipo, bool? estado, string? orden, int pagina, int tamano)
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

            var total = await consulta.CountAsync();
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

            var items = (await conFechas
                    .Skip((pagina - 1) * tamano)
                    .Take(tamano)
                    .ToListAsync())
                .Select(Map)
                .ToList();

            return new UsuarioPaginaResponse
            {
                items = items,
                pagina = pagina,
                tamano = tamano,
                total = total,
                totalPaginas = totalPaginas,
                resumen = await ResumenAsync()
            };
        }

        // Nunca devolver la entidad: incluye el hash de la contraseña y tokens.
        public async Task<List<UsuarioResponse>> FiltrarPorTipoAsync(int tipo) =>
            (await ConFechas(_context.Usuario.AsNoTracking().Where(u => u.fk_Tipo_Usuario == tipo)).ToListAsync())
                .Select(Map)
                .ToList();

        public async Task<List<UsuarioResponse>> UsuariosActivosAsync() =>
            (await ConFechas(_context.Usuario.AsNoTracking().Where(u => u.estado)).ToListAsync())
                .Select(Map)
                .ToList();

        // ==================================================================
        // Altas
        // ==================================================================

        /// <summary>Alta hecha por un Admin: respeta el tipo elegido.</summary>
        public async Task<UsuarioResponse> CreateUsuarioAsync(CreateUsuarioRequest request, string actorEmail)
        {
            var email = request.email.Trim();

            if (await _context.Usuario.AnyAsync(u => u.email == email))
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
            await _context.SaveChangesAsync();

            // La fecha de registro sale de este registro de auditoría.
            await _auditoria.RegistrarAsync(Entidad, "CREATE", $"Usuario {usuario.email} creado", actorEmail, usuario.id);

            return (await GetUsuarioByIdAsync(usuario.id))!;
        }

        /// <summary>Registro público: siempre crea un Cliente.</summary>
        public async Task<UsuarioResponse> RegisterUsuarioAsync(RegisterUsuarioRequest request)
        {
            // El controlador ya valida el DTO; se repite aquí para que ningún llamador lo salte.
            if (!request.aceptaTerminos || string.IsNullOrWhiteSpace(request.versionTerminos))
                throw new InvalidOperationException(RegisterUsuarioRequest.MensajeTerminos);

            bool emailExiste = await _context.Usuario.AnyAsync(u => u.email == request.email);

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
            await _context.SaveChangesAsync();

            await GuardarAceptacionTerminosAsync(usuario.id, request.versionTerminos.Trim());

            await _auditoria.RegistrarAsync(Entidad, "CREATE", $"Usuario {usuario.email} registrado", "sistema", usuario.id);

            return (await GetUsuarioByIdAsync(usuario.id))!;
        }

        /// <summary>
        /// Guarda la versión y la fecha/hora (UTC) en que el usuario aceptó los términos.
        /// Si aún no existe la tabla (falta 2026-11-16_usuario_terminos.sql) el registro sigue y queda aviso en el log.
        /// </summary>
        private async Task GuardarAceptacionTerminosAsync(int usuarioId, string version)
        {
            var aceptacion = new UsuarioTerminosModel
            {
                fk_Usuario = usuarioId,
                version_Terminos = version,
                fecha_Aceptacion = DateTime.UtcNow
            };

            _context.UsuarioTerminos.Add(aceptacion);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 208 })
            {
                _context.Entry(aceptacion).State = EntityState.Detached;
                _logger.LogWarning("No existe la tabla USUARIO_TERMINOS (falta 2026-11-16_usuario_terminos.sql); " +
                    "no se guardó la aceptación de términos del usuario {UsuarioId}.", usuarioId);
            }
        }

        // ==================================================================
        // Cambios (todos quedan en AUDITORIA)
        // ==================================================================

        /// <summary>Activa o desactiva. Un Admin no puede desactivarse a sí mismo.</summary>
        /// <exception cref="InvalidOperationException">Si el Admin intenta desactivarse.</exception>
        public async Task<UsuarioResponse?> CambiarEstadoAsync(int id, bool activo, int actorId, string actorEmail)
        {
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.id == id);

            if (usuario == null)
                return null;

            if (!activo && id == actorId)
                throw new InvalidOperationException("No puede desactivar su propia cuenta.");

            if (usuario.estado != activo)
            {
                usuario.estado = activo;
                await _context.SaveChangesAsync();
                _validadorSesion.Invalidar(usuario.id); // desactivar revoca sus tokens al instante

                await _auditoria.RegistrarAsync(Entidad, activo ? "ENABLE" : "DISABLE",
                    $"Usuario {usuario.email} {(activo ? "activado" : "desactivado")}", actorEmail, usuario.id);
            }

            return await GetUsuarioByIdAsync(id);
        }

        /// <summary>
        /// Edita nombre, tipo y (opcionalmente) estado. El correo no se modifica aquí.
        /// Un Admin no puede quitarse el rol de administrador ni desactivarse a sí mismo.
        /// </summary>
        public async Task<UsuarioResponse?> UpdateUsuarioAsync(int id, UpdateUsuarioRequest request, int actorId, string actorEmail)
        {
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.id == id);

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

                await _context.SaveChangesAsync();
                _validadorSesion.Invalidar(usuario.id); // un cambio de rol o estado revoca sus tokens

                var detalle = Recortar($"Usuario {usuario.email} actualizado ({string.Join(", ", cambios)})");
                await _auditoria.RegistrarAsync(Entidad, "UPDATE", detalle, actorEmail, usuario.id);
            }

            return await GetUsuarioByIdAsync(id);
        }

        // ==================================================================
        // Perfil propio (la identidad la pone el controlador desde el token)
        // ==================================================================

        public async Task<PerfilResponse?> ObtenerPerfilAsync(int usuarioId)
        {
            var u = await GetUsuarioByIdAsync(usuarioId);
            return u == null ? null : new PerfilResponse
            {
                nombre = u.nombre,
                email = u.email,
                tipo = u.tipo,
                fecha_Registro = u.fecha_Registro,
                ultimo_Login = u.ultimo_Login
            };
        }

        /// <summary>
        /// Cambia el nombre del propio usuario. No toca el sello: el nombre no da permisos,
        /// así que sus sesiones siguen válidas.
        /// </summary>
        /// <exception cref="InvalidOperationException">Si el nombre no es válido.</exception>
        public async Task<PerfilResponse?> ActualizarNombrePropioAsync(int usuarioId, string? nombre, string actorEmail)
        {
            var (limpio, error) = NombreUsuario.Normalizar(nombre);
            if (limpio == null)
                throw new InvalidOperationException(error);

            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.id == usuarioId && u.estado);
            if (usuario == null)
                return null;

            if (usuario.nombre != limpio)
            {
                usuario.nombre = limpio;
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(Entidad, "UPDATE",
                    Recortar($"Usuario {usuario.email} actualizado (nombre, desde su perfil)"), actorEmail, usuario.id);
            }

            return await ObtenerPerfilAsync(usuarioId);
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

        private async Task<UsuarioResumenResponse> ResumenAsync()
        {
            var porTipo = await _context.Usuario
                .GroupBy(u => u.fk_Tipo_Usuario)
                .Select(g => new { tipo = g.Key, cantidad = g.Count(), inactivos = g.Count(u => !u.estado) })
                .ToListAsync();

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
