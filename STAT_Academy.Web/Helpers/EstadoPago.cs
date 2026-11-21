namespace STAT_Academy.Web.Helpers
{
    /// <summary>Textos y estilos de los estados de facturas y comprobantes de pago.</summary>
    public static class EstadoPago
    {
        /// <summary>Estado de la factura (PENDIENTE_PAGO, EN_VERIFICACION, RECHAZADA, PAGADA).</summary>
        public static (string texto, string clase) Factura(string? estado) => estado switch
        {
            "PAGADA" => ("Pagada", "tutor-pill--ok"),
            "EN_VERIFICACION" => ("Pago en verificación", ""),
            "RECHAZADA" => ("Pago rechazado", "pill--rechazo"),
            _ => ("Pendiente de pago", "tutor-pill--alerta")
        };

        /// <summary>Estado del comprobante (PENDIENTE, APROBADO, RECHAZADO).</summary>
        public static (string texto, string clase) Comprobante(string? estado) => estado switch
        {
            "APROBADO" => ("Aprobado", "tutor-pill--ok"),
            "RECHAZADO" => ("Rechazado", "pill--rechazo"),
            _ => ("Pendiente de verificación", "tutor-pill--alerta")
        };

        public static string Metodo(string? metodo) =>
            metodo == "TRANSFERENCIA" ? "Transferencia / depósito" : "SINPE Móvil";

        private static readonly TimeSpan HusoCostaRica = TimeSpan.FromHours(-6);

        /// <summary>Fecha UTC de la API en hora de Costa Rica (UTC-6 todo el año).</summary>
        public static string Fecha(DateTime utc, string formato = "dd/MM/yyyy HH:mm") =>
            DateTime.SpecifyKind(utc, DateTimeKind.Utc).Add(HusoCostaRica).ToString(formato);
    }
}
