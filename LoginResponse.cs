using System.Text.Json.Serialization;
using System.Text.Json.Serialization;

namespace STAT_Academy.DTOs.Login

{
    public class LoginResponse
    {
        public int id { get; set; }
        public string email { get; set; } = "";
        public string nombre { get; set; } = "";
        public int fk_Tipo_Usuario { get; set; }
    }
}