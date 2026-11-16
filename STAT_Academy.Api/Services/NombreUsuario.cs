using System.Text.RegularExpressions;

namespace STAT_Academy.Api.Services
{
    /// <summary>Limpieza y validación del nombre que el usuario escribe para sí mismo.</summary>
    public static partial class NombreUsuario
    {
        public const int Minimo = 2;
        public const int Maximo = 150;

        /// <summary>
        /// Quita espacios al inicio y al final y junta los espacios repetidos.
        /// Devuelve el error si queda fuera de 2–150 caracteres o trae caracteres de control.
        /// </summary>
        public static (string? nombre, string? error) Normalizar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return (null, "El nombre es obligatorio.");

            if (texto.Any(c => char.IsControl(c) && !char.IsWhiteSpace(c)))
                return (null, "El nombre contiene caracteres no permitidos.");

            var nombre = Espacios().Replace(texto.Trim(), " ");

            if (nombre.Length < Minimo || nombre.Length > Maximo)
                return (null, $"El nombre debe tener entre {Minimo} y {Maximo} caracteres.");

            return (nombre, null);
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex Espacios();
    }
}
