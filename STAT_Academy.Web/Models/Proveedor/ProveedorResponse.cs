using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Proveedor;

public class ProveedorResponse
{
    public int id { get; set; }
    [Display(Name = "Nombre")]
    public string nombre { get; set; } = string.Empty;
    [Display(Name = "Contacto")]
    public string contacto { get; set; } = string.Empty;
    [Display(Name = "Teléfono")]
    public string telefono { get; set; } = string.Empty;
    [Display(Name = "Correo electrónico")]
    public string email { get; set; } = string.Empty;
    [Display(Name = "Estado")]
    public bool estado { get; set; }
    [Display(Name = "Fecha de registro")]
    public DateTime fecha_creacion { get; set; }
    [Display(Name = "Última edición")]
    public DateTime fecha_edicion { get; set; }
}