namespace STAT_Academy.Web.Models.Usuarios
{
    public class UsuarioResponse
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public int tipo { get; set; }
        public bool activo { get; set; }
    }
}