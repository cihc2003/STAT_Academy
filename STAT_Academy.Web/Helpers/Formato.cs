using System.Globalization;

namespace STAT_Academy.Web.Helpers
{
    /// <summary>
    /// Formatos de presentación independientes de la configuración regional del servidor
    /// (en Azure el servidor suele estar en inglés y "C" mostraría dólares).
    /// </summary>
    public static class Formato
    {
        private static readonly CultureInfo CostaRica = CultureInfo.GetCultureInfo("es-CR");

        /// <summary>Precio en colones: ₡45 000,00</summary>
        public static string Colones(decimal monto) => monto.ToString("C", CostaRica);

        /// <summary>Fecha legible: 5 de enero de 2027</summary>
        public static string FechaLarga(DateTime fecha) => fecha.ToString("d 'de' MMMM 'de' yyyy", CostaRica);

        /// <summary>Fecha sin conversión de zona, corta: 3 feb 2027.</summary>
        public static string FechaCorta(DateTime fecha) => fecha.ToString("d MMM yyyy", CostaRica).Replace(".", "");

        /// <summary>Mes abreviado sin punto: feb.</summary>
        public static string MesCorto(DateTime fecha) => fecha.ToString("MMM", CostaRica).Replace(".", "");

        /// <summary>Duración en semanas: "1 semana", "8 semanas".</summary>
        public static string Semanas(int semanas) => semanas == 1 ? "1 semana" : $"{semanas} semanas";

        /// <summary>Precio o "Gratis" si es cero.</summary>
        public static string PrecioCurso(decimal monto) => monto > 0 ? Colones(monto) : "Gratis";

        /// <summary>Iniciales para un respaldo visual sin imagen: "Excel Avanzado" → "EA".</summary>
        public static string Iniciales(string? texto)
        {
            var palabras = (texto ?? "")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(p => p.Length > 2 && char.IsLetterOrDigit(p[0]))
                .Take(2)
                .Select(p => char.ToUpper(p[0], CostaRica))
                .ToArray();

            return palabras.Length > 0 ? new string(palabras) : "C";
        }

        /// <summary>
        /// Fecha y hora actual en Costa Rica. Las fechas de las tareas y entregas ya vienen en
        /// hora de Costa Rica desde la API, así que se comparan contra este valor (no contra DateTime.Now).
        /// </summary>
        public static DateTime AhoraCostaRica => DateTime.UtcNow.AddHours(-6);

        /// <summary>Fecha y hora local (sin conversión): 14/11/2026 09:30.</summary>
        public static string FechaHoraLocal(DateTime? fecha, string sinDato = "—") =>
            fecha == null ? sinDato : fecha.Value.ToString("dd/MM/yyyy HH:mm", CostaRica);

        /// <summary>Nota sobre 100 sin ceros de sobra: 85, 92,5.</summary>
        public static string Nota(decimal? nota, string sinDato = "—") =>
            nota == null ? sinDato : nota.Value.ToString("0.##", CostaRica);

        /// <summary>Convierte una fecha UTC de la API a hora de Costa Rica (UTC-6 todo el año).</summary>
        public static DateTime HoraCostaRica(DateTime utc) =>
            (utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : utc).AddHours(-6);

        /// <summary>Fecha y hora en Costa Rica: 14/11/2026 09:30. Sin dato: el texto indicado.</summary>
        public static string FechaHora(DateTime? utc, string sinDato = "Sin registro") =>
            utc == null ? sinDato : HoraCostaRica(utc.Value).ToString("dd/MM/yyyy HH:mm", CostaRica);

        /// <summary>Fecha en Costa Rica: 14 nov 2026.</summary>
        public static string Fecha(DateTime? utc, string sinDato = "Sin registro") =>
            utc == null ? sinDato : HoraCostaRica(utc.Value).ToString("dd MMM yyyy", CostaRica).Replace(".", "");

        /// <summary>Tiempo transcurrido en palabras: "hace 3 días", "hace 2 h", "hoy".</summary>
        public static string Hace(DateTime? utc, string sinDato = "Nunca")
        {
            if (utc == null)
                return sinDato;

            var valor = utc.Value.Kind == DateTimeKind.Local ? utc.Value.ToUniversalTime() : utc.Value;
            var lapso = DateTime.UtcNow - valor;

            if (lapso.TotalMinutes < 1) return "hace un momento";
            if (lapso.TotalMinutes < 60) return $"hace {(int)lapso.TotalMinutes} min";
            if (lapso.TotalHours < 24) return $"hace {(int)lapso.TotalHours} h";
            if (lapso.TotalDays < 2) return "ayer";
            if (lapso.TotalDays < 30) return $"hace {(int)lapso.TotalDays} días";
            if (lapso.TotalDays < 365)
            {
                var meses = (int)(lapso.TotalDays / 30);
                return meses == 1 ? "hace 1 mes" : $"hace {meses} meses";
            }

            var anios = (int)(lapso.TotalDays / 365);
            return anios == 1 ? "hace 1 año" : $"hace {anios} años";
        }
    }
}
