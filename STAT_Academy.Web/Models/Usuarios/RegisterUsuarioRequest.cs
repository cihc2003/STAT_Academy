using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Usuarios
{
    public class RegisterUsuarioRequest
    {
        public string nombre { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string password { get; set; } = string.Empty;
        public int tipo { get; set; }
    }
}