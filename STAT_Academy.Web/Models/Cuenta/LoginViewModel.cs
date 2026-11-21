using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Cuenta
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Escriba su correo electrónico.")]
        [EmailAddress(ErrorMessage = "Escriba un correo electrónico válido.")]
        [Display(Name = "Correo electrónico")]
        public string email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escriba su contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string password { get; set; } = string.Empty;
    }
}
