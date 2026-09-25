namespace STAT_Academy.Web.Models.Usuarios;

public class CreateUsuarioRequest
{
    public string nombre { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public int fk_Tipo_Usuario { get; set; }
    public string password { get; set; } = string.Empty;
}