using System.Net;

namespace STAT_Academy.Web.Models.Api;

public record ApiResultado<T>(bool Exitoso, T? Datos, string? Mensaje, HttpStatusCode Codigo)
{
    public static ApiResultado<T> Correcto(T datos) => new(true, datos, null, HttpStatusCode.OK);
    public static ApiResultado<T> Error(string mensaje, HttpStatusCode codigo) => new(false, default, mensaje, codigo);
}
