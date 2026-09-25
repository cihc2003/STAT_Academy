namespace STAT_Academy.Web.Models.Proveedor;

public class ProveedorResponse
{
    public int id { get; set; }
    public string nombre { get; set; } = string.Empty;
    public string contacto { get; set; } = string.Empty;
    public string telefono { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public bool estado { get; set; }
    public DateTime fecha_creacion { get; set; }
    public DateTime fecha_edicion { get; set; }
}