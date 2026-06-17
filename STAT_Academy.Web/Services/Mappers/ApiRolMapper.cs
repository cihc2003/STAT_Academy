using STAT_Academy.Web.Data;

namespace STAT_Academy.Web.Services.Mappers;

public static class ApiRolMapper
{
    public static string ObtenerRol(int tipoUsuario) => tipoUsuario switch
    {
        1 => SeedData.AdminRole,
        2 => SeedData.TutorRole,
        _ => SeedData.StudentRole
    };

    public static int ObtenerTipoUsuario(string rol) => rol switch
    {
        SeedData.AdminRole => 1,
        SeedData.TutorRole => 2,
        _ => 3
    };
}
