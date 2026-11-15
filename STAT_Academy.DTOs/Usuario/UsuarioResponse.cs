namespace STAT_Academy.DTOs.Usuarios
{
    public class UsuarioResponse
    {
        public int id { get; set; }
        public string email { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public bool estado { get; set; }
        public int intentos_login { get; set; }
        /// <summary>Último inicio de sesión correcto (UTC).</summary>
        public DateTime? ultimo_Login { get; set; }
        public int fk_Tipo_Usuario { get; set; }
        /// <summary>"Admin", "Tutor", "Cliente" o "Usuario".</summary>
        public string tipo { get; set; } = string.Empty;
        /// <summary>Fecha de registro según la tabla AUDITORIA (UTC); null si no hay registro.</summary>
        public DateTime? fecha_Registro { get; set; }
        /// <summary>Última modificación según la tabla AUDITORIA (UTC).</summary>
        public DateTime? fecha_Edicion { get; set; }
    }

    /// <summary>Una página de usuarios con los filtros aplicados y el resumen por tipo.</summary>
    public class UsuarioPaginaResponse
    {
        public List<UsuarioResponse> items { get; set; } = new();
        public int pagina { get; set; }
        public int tamano { get; set; }
        /// <summary>Total de usuarios que cumplen los filtros.</summary>
        public int total { get; set; }
        public int totalPaginas { get; set; }
        public UsuarioResumenResponse resumen { get; set; } = new();
    }

    /// <summary>Totales de todos los usuarios (sin filtros).</summary>
    public class UsuarioResumenResponse
    {
        public int total { get; set; }
        public int admins { get; set; }
        public int tutores { get; set; }
        public int clientes { get; set; }
        public int inactivos { get; set; }
    }
}
