namespace STAT_Academy.Web.Models.Cuenta
{
    /// <summary>Respuesta de POST api/Login.</summary>
    public class LoginResult
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public int fk_Tipo_Usuario { get; set; }
        public string token { get; set; } = string.Empty;
        public DateTime expiraEn { get; set; }
    }
}
