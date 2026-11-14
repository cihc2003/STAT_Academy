using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Cuenta
{
    /// <summary>
    /// Formulario público de registro. Solo nombre, email y password viajan a la API
    /// (como RegisterUsuarioRequest); la confirmación se valida aquí y no se envía.
    /// </summary>
    public class RegistroViewModel
    {
        [Required(ErrorMessage = "Escriba su nombre completo.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        [Display(Name = "Nombre completo")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escriba su correo electrónico.")]
        [EmailAddress(ErrorMessage = "Escriba un correo electrónico válido.")]
        [Display(Name = "Correo electrónico")]
        public string email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escriba una contraseña.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme la contraseña.")]
        [DataType(DataType.Password)]
        [Compare(nameof(password), ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar contraseña")]
        public string confirmarPassword { get; set; } = string.Empty;
    }
}
