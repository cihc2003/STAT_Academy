namespace STAT_Academy.DTOs.Login
{
    public class LoginResponse
    {
        public int id { get; set; }
        public string email { get; set; } = "";
        public string nombre { get; set; } = "";
        public int fk_Tipo_Usuario { get; set; }

        /// <summary>JWT que el cliente debe enviar en "Authorization: Bearer ...".</summary>
        public string token { get; set; } = "";

        /// <summary>Momento (UTC) en que expira el token.</summary>
        public DateTime expiraEn { get; set; }
    }
}
