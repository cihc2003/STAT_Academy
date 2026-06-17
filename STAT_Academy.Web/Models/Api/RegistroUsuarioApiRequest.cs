using System.Text.Json.Serialization;

namespace STAT_Academy.Web.Models.Api;

public record RegistroUsuarioApiRequest(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("nombre")] string Nombre,
    [property: JsonPropertyName("password")] string Password);
