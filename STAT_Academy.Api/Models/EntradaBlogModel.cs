using System.ComponentModel.DataAnnotations.Schema;
using STAT_Academy.Api.Models;

public class EntradaBlogModel
{
    public int id { get; set; }

    public string titulo { get; set; } = "";

    public string contenido { get; set; } = "";

    public bool estado { get; set; }

    public int fk_Autor { get; set; }

    /// <summary>Momento (UTC) en que se creó la entrada. Requiere el script 2026-11-10.</summary>
    [Column("Fecha_Creacion")]
    public DateTime fecha_Creacion { get; set; }

    /// <summary>Última edición (UTC); null si nunca se editó.</summary>
    [Column("Fecha_Edicion")]
    public DateTime? fecha_Edicion { get; set; }

    public UsuarioModel? Autor { get; set; }

    public List<EntradaBlogArchivoModel> Archivos { get; set; } = new();
}
