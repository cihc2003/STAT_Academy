using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Cuenta
{
    public class LoginViewModel
    {
        public string email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string password { get; set; }
    }
}
