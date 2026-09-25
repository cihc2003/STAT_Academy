using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Correo
{
    public class ConfirmarCambioCorreoViewModel
    {
        [Required]
        public string token { get; set; } = string.Empty;
    }
}