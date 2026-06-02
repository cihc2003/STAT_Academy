namespace STAT_Academy.Api.Models
{
    public class UsuarioModel
    {
        public int id { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public bool? estado { get; set; }
        public int? intentos_login { get; set; }
        public DateTime? fecha_creacion { get; set; }
        public DateTime? fecha_edicion { get; set; }
        public DateTime? ultimo_Login { get; set; }
        public int? fk_Tipo_Usuario { get; set; }
    }
}
