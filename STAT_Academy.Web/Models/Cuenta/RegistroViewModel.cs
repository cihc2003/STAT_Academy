using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Cuenta
{
    /// <summary>
    /// Formulario público de registro. Nombre, email, password y la aceptación de los términos
    /// viajan a la API (como RegisterUsuarioRequest); la confirmación se valida aquí y no se envía.
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

        /// <summary>Desmarcada por defecto; sin ella el registro se rechaza también en el servidor.</summary>
        // Required solo fija el mensaje en español de la validación del navegador; DebeMarcarse exige true en el servidor.
        [Required(ErrorMessage = "Para crear la cuenta debe aceptar los términos y condiciones y la política de cookies.")]
        [DebeMarcarse(ErrorMessage = "Para crear la cuenta debe aceptar los términos y condiciones y la política de cookies.")]
        public bool aceptaTerminos { get; set; }
    }
}
