namespace STAT_Academy.Web.Services
{
    /// <summary>
    /// La API rechazó el JWT del usuario (vencido o revocado). La lanza <see cref="BearerTokenHandler"/>
    /// y <see cref="Filtros.ErroresApiFilter"/> la convierte en cierre de sesión + redirección al login.
    /// No hereda de HttpRequestException a propósito, para que ningún servicio la trague por error.
    /// </summary>
    public class ApiSesionExpiradaException : Exception
    {
        public ApiSesionExpiradaException()
            : base("La API rechazó el token de la sesión (401).")
        {
        }
    }
}
