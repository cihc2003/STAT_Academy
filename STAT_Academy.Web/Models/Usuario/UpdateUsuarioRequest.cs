using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Usuarios;

/// <summary>Cuerpo de PUT api/Usuario/{id}: el correo no se envía porque no se puede cambiar.</summary>
public class UpdateUsuarioRequest
{
    public string nombre { get; set; } = string.Empty;
    public int fk_Tipo_Usuario { get; set; }
}

/// <summary>Formulario de alta y edición de usuarios en el panel de administración.</summary>
public class UsuarioFormViewModel
{
    /// <summary>null = alta.</summary>
    public int? Id { get; set; }

    [Display(Name = "Nombre completo")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Solo se usa en el alta; al editar se muestra pero no se envía.</summary>
    [Display(Name = "Correo electrónico")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(150, ErrorMessage = "El correo admite como máximo 150 caracteres.")]
    public string? Email { get; set; }

    [Display(Name = "Tipo de usuario")]
    [Range(1, 3, ErrorMessage = "Seleccione un tipo de usuario.")]
    public int Tipo { get; set; } = TiposUsuario.Cliente;

    [Display(Name = "Contraseña temporal")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string? Password { get; set; }

    // Solo lectura en la edición
    public bool Activo { get; set; } = true;
    public DateTime? FechaRegistro { get; set; }
    public DateTime? UltimoLogin { get; set; }

    /// <summary>True cuando el Admin se edita a sí mismo: no puede cambiar su tipo.</summary>
    public bool EsUsuarioActual { get; set; }

    public bool EsEdicion => Id is > 0;
}
