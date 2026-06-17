using System.Text.Json.Serialization;

namespace STAT_Academy.Web.Models.Api;

public record UsuarioApiResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("nombre")] string Nombre,
    [property: JsonPropertyName("estado")] bool Estado,
    [property: JsonPropertyName("intentos_login")] int IntentosLogin,
    [property: JsonPropertyName("fecha_creacion")] DateTime FechaCreacion,
    [property: JsonPropertyName("fecha_edicion")] DateTime? FechaEdicion,
    [property: JsonPropertyName("ultimo_Login")] DateTime? UltimoLogin,
    [property: JsonPropertyName("fk_Tipo_Usuario")] int TipoUsuario);
