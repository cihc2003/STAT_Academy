using System.Text.Json.Serialization;

namespace STAT_Academy.Web.Models.Usuarios;

public class UsuarioResponse
{
    public int id { get; set; }
    public string nombre { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public int fk_Tipo_Usuario { get; set; }

    [JsonPropertyName("estado")]
    public bool activo { get; set; }
}