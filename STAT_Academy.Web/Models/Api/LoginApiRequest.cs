using System.Text.Json.Serialization;

namespace STAT_Academy.Web.Models.Api;

public record LoginApiRequest(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password);
