using System.Text.Json.Serialization;

namespace STAT_Academy.Web.Models.Api;

public record ActualizarUsuarioApiRequest(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("nombre")] string Nombre,
    [property: JsonPropertyName("fk_Tipo_Usuario")] int TipoUsuario,
    [property: JsonPropertyName("estado")] bool Estado);
