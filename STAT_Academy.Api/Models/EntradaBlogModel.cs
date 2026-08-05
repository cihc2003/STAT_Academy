using STAT_Academy.Api.Models;

public class EntradaBlogModel
{
    public int id { get; set; }

    public string titulo { get; set; } = "";

    public string contenido { get; set; } = "";

    public DateTime fecha_creacion { get; set; }

    public DateTime? fecha_edicion { get; set; }

    public bool estado { get; set; }

    public int fk_Autor { get; set; }


    public UsuarioModel? Autor { get; set; }
}