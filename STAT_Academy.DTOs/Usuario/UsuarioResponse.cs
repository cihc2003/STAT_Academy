namespace STAT_Academy.DTOs.Usuarios
{
    public class UsuarioResponse
    {
        public int id { get; set; }
        public string email { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public bool estado { get; set; }
        public int intentos_login { get; set; }
        public DateTime fecha_creacion { get; set; }
        public DateTime? fecha_edicion { get; set; }
        public DateTime? ultimo_Login { get; set; }
        public int fk_Tipo_Usuario { get; set; }
    }
}