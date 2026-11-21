using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Web.Models.Cuenta
{
    /// <summary>
    /// Casilla obligatoria (p. ej. aceptar los términos): solo es válido el valor true.
    /// Se valida en el servidor; en el navegador, site.js deshabilita el envío mientras no se marque.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DebeMarcarseAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value) => value is true;
    }
}
