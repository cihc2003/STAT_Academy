using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Contrasena
{
    public class SolicitarRecuperacionViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        [Display(Name = "Correo electrónico")]
        public string email { get; set; } = string.Empty;
    }
}