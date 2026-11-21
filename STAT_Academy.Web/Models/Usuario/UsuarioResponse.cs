using System.Text.Json.Serialization;

namespace STAT_Academy.Web.Models.Usuarios;

public class UsuarioResponse
{
    /// <summary>Solo para armar enlaces; no se muestra en pantalla.</summary>
    public int id { get; set; }
    public string nombre { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public int fk_Tipo_Usuario { get; set; }

    [JsonPropertyName("estado")]
    public bool activo { get; set; }

    public int intentos_login { get; set; }

    /// <summary>Fechas en UTC (se muestran en hora de Costa Rica con Helpers.Formato).</summary>
    public DateTime? ultimo_Login { get; set; }
    public DateTime? fecha_Registro { get; set; }
    public DateTime? fecha_Edicion { get; set; }

    public string TipoTexto => TiposUsuario.Nombre(fk_Tipo_Usuario);
}

public class UsuarioPaginaResponse
{
    public List<UsuarioResponse> items { get; set; } = [];
    public int pagina { get; set; } = 1;
    public int tamano { get; set; } = 10;
    public int total { get; set; }
    public int totalPaginas { get; set; } = 1;
    public UsuarioResumen resumen { get; set; } = new();
}

public class UsuarioResumen
{
    public int total { get; set; }
    public int admins { get; set; }
    public int tutores { get; set; }
    public int clientes { get; set; }
    public int inactivos { get; set; }
}

/// <summary>Filtros de la lista de usuarios (vienen en la query string).</summary>
public class UsuarioFiltros
{
    public static readonly int[] TamanosPagina = [10, 25, 50];

    public string? Buscar { get; set; }
    public int? Tipo { get; set; }
    public bool? Activo { get; set; }
    /// <summary>nombre, nombre_desc, registro, registro_desc, login o login_desc.</summary>
    public string? Orden { get; set; }
    public int Pagina { get; set; } = 1;
    public int Tamano { get; set; } = 10;

    public bool HayFiltros => !string.IsNullOrWhiteSpace(Buscar) || Tipo != null || Activo != null;

    /// <summary>Valores de ruta para un enlace que conserva los filtros y cambia lo indicado.</summary>
    public Dictionary<string, string> Ruta(int? pagina = null, string? orden = null, int? tamano = null)
    {
        var ruta = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(Buscar)) ruta["buscar"] = Buscar.Trim();
        if (Tipo != null) ruta["tipo"] = Tipo.Value.ToString();
        if (Activo != null) ruta["activo"] = Activo.Value ? "true" : "false";

        var o = orden ?? Orden;
        if (!string.IsNullOrWhiteSpace(o)) ruta["orden"] = o;

        var t = tamano ?? Tamano;
        if (t != 10) ruta["tamano"] = t.ToString();

        var p = pagina ?? Pagina;
        if (p > 1) ruta["pagina"] = p.ToString();

        return ruta;
    }
}

public class UsuariosIndexViewModel
{
    public UsuarioPaginaResponse Pagina { get; set; } = new();
    public UsuarioFiltros Filtros { get; set; } = new();
    /// <summary>Id del Admin conectado: no puede desactivarse a sí mismo.</summary>
    public int UsuarioActualId { get; set; }
}

public static class TiposUsuario
{
    public const int Admin = 1;
    public const int Tutor = 2;
    public const int Cliente = 3;

    public static readonly (int valor, string nombre)[] Todos =
    [
        (Admin, "Administrador"),
        (Tutor, "Tutor"),
        (Cliente, "Cliente")
    ];

    public static string Nombre(int tipo) => tipo switch
    {
        Admin => "Administrador",
        Tutor => "Tutor",
        Cliente => "Cliente",
        _ => "Usuario"
    };

    /// <summary>Clase CSS de la etiqueta del tipo.</summary>
    public static string Clase(int tipo) => tipo switch
    {
        Admin => "usuario-tipo--admin",
        Tutor => "usuario-tipo--tutor",
        _ => "usuario-tipo--cliente"
    };
}
