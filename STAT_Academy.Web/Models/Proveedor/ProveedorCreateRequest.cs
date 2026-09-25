namespace STAT_Academy.Web.Models.Proveedor;

public class ProveedorCreateRequest
{
    public string nombre { get; set; } = string.Empty;
    public string contacto { get; set; } = string.Empty;
    public string telefono { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
}