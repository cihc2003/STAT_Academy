namespace STAT_Academy.Api.Auth
{
    /// <summary>
    /// Nombres de rol usados en el token JWT. Coinciden con los que usa STAT_Academy.Web.
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Tutor = "Tutor";
        public const string Cliente = "Cliente";
        public const string Usuario = "Usuario";

        public const string AdminOTutor = Admin + "," + Tutor;

        public static string DesdeTipoUsuario(int fkTipoUsuario) => fkTipoUsuario switch
        {
            1 => Admin,
            2 => Tutor,
            3 => Cliente,
            _ => Usuario
        };
    }
}
