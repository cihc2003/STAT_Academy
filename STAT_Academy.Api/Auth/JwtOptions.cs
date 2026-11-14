namespace STAT_Academy.Api.Auth
{
    public class JwtOptions
    {
        public const string Seccion = "Jwt";

        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;

        /// <summary>Clave HMAC en Base64 (mínimo 32 bytes). Nunca en appsettings.json.</summary>
        public string Key { get; set; } = string.Empty;

        public int ExpiracionMinutos { get; set; } = 60;
    }
}
