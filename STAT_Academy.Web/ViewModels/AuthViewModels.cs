using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [Display(Name = "Nombre completo")]
    [StringLength(120, ErrorMessage = "El nombre completo no puede superar los 120 caracteres.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [Phone(ErrorMessage = "Ingresa un teléfono válido.")]
    [Display(Name = "Teléfono")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica si deseas recordar la sesión.")]
    [Display(Name = "Recordarme")]
    public bool RememberMe { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;
}

public class CartItemViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal => Price * Quantity;
}

public class UserAdminViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [Display(Name = "Nombre completo")]
    [StringLength(120, ErrorMessage = "El nombre completo no puede superar los 120 caracteres.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Ingresa un teléfono válido.")]
    [Display(Name = "Teléfono")]
    public string? PhoneNumber { get; set; }

    [StringLength(30, ErrorMessage = "El documento no puede superar los 30 caracteres.")]
    [Display(Name = "Documento")]
    public string DocumentId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es obligatorio.")]
    [Display(Name = "Rol")]
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
