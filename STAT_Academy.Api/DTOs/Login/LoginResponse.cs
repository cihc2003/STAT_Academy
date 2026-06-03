namespace STAT_Academy.Api.DTOs.Login
{
    public class LoginResponse
    {
        public int id { get; set; }
        public string email { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public int fk_Tipo_Usuario { get; set; }
    }
}