using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models
{
    public class RegistroUsuarioModel
    {
        [Required]
        [EmailAddress]
        public string email { get; set; }


        [Required]
        public string nombre { get; set; }


        [Required]
        [MinLength(6)]
        public string password { get; set; }


        [Required]
        [Compare("password")]
        public string confirmarPassword { get; set; }
    }
}