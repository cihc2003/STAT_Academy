using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using STAT_Academy.DTOs.Factura;
using System.Globalization;
using System.Net;

namespace STAT_Academy.Api.Services
{
    /// <summary>
    /// Envío de correos por SMTP con MailKit (System.Net.Mail.SmtpClient está obsoleto
    /// y oculta la causa real de los errores, p. ej. credenciales rechazadas por Gmail).
    /// </summary>
    public class CorreoService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CorreoService> _logger;

        public CorreoService(IConfiguration configuration, ILogger<CorreoService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Task EnviarRecuperacionContrasena(string correoDestino, string token)
        {
            var configuracion = ObtenerConfiguracion();

            var enlace =
                $"{configuracion.WebBaseUrl}/Cuenta/RestablecerContrasena" +
                $"?token={Uri.EscapeDataString(token)}";

            return Enviar(configuracion, correoDestino, "Recuperación de contraseña", $"""
                <h2>Recuperación de contraseña</h2>

                <p>
                    Recibimos una solicitud para restablecer
                    tu contraseña de STAT Academy.
                </p>

                <p><a href="{enlace}">Restablecer mi contraseña</a></p>

                <p>
                    Este enlace vence en 30 minutos y solamente
                    puede utilizarse una vez.
                </p>

                <p>
                    Si no realizaste esta solicitud,
                    puedes ignorar este mensaje.
                </p>
                """);
        }

        public Task EnviarConfirmacionRegistro(string correoDestino, string nombre)
        {
            var configuracion = ObtenerConfiguracion();
            var enlace = $"{configuracion.WebBaseUrl}/Cuenta/Login";

            return Enviar(configuracion, correoDestino, "Bienvenido a STAT Academy: registro confirmado", $"""
                <h2>¡Bienvenido a STAT Academy, {WebUtility.HtmlEncode(nombre)}!</h2>

                <p>
                    Su cuenta fue creada correctamente con esta
                    dirección de correo.
                </p>

                <p><a href="{enlace}">Iniciar sesión</a></p>

                <p>
                    Si usted no creó esta cuenta, responda a este
                    mensaje para que podamos revisarlo.
                </p>
                """);
        }

        public Task EnviarAvisoCambioContrasena(string correoDestino, string nombre)
        {
            var configuracion = ObtenerConfiguracion();
            var enlace = $"{configuracion.WebBaseUrl}/Cuenta/Forgotpassword";

            return Enviar(configuracion, correoDestino, "Su contraseña de STAT Academy fue cambiada", $"""
                <h2>Hola, {WebUtility.HtmlEncode(nombre)}</h2>

                <p>
                    La contraseña de su cuenta de STAT Academy
                    se cambió el {DateTime.UtcNow:dd/MM/yyyy HH:mm} (UTC).
                </p>

                <p>
                    Si no fue usted, recupere el acceso de inmediato:
                    <a href="{enlace}">restablecer mi contraseña</a>.
                </p>
                """);
        }

        public async Task EnviarAlertaStockBajo(
            IReadOnlyCollection<string> destinos,
            IReadOnlyCollection<(string nombre, int stock, int minimo)> productos)
        {
            var configuracion = ObtenerConfiguracion();
            var enlace = $"{configuracion.WebBaseUrl}/Admin/Products?stockBajo=true";

            var filas = string.Concat(productos.Select(p =>
                $"<tr><td style=\"padding:6px 12px\">{WebUtility.HtmlEncode(p.nombre)}</td>" +
                $"<td style=\"padding:6px 12px;text-align:center\"><strong>{p.stock}</strong></td>" +
                $"<td style=\"padding:6px 12px;text-align:center\">{p.minimo}</td></tr>"));

            var html = $"""
                <h2>Stock bajo en la tienda</h2>

                <p>
                    {(productos.Count == 1 ? "Este producto llegó" : "Estos productos llegaron")}
                    a su stock mínimo o por debajo:
                </p>

                <table style="border-collapse:collapse;border:1px solid #dce5f2">
                    <thead>
                        <tr style="background:#f4f7fb">
                            <th style="padding:6px 12px;text-align:left">Producto</th>
                            <th style="padding:6px 12px">Existencias</th>
                            <th style="padding:6px 12px">Mínimo</th>
                        </tr>
                    </thead>
                    <tbody>{filas}</tbody>
                </table>

                <p><a href="{enlace}">Ver productos con stock bajo</a></p>
                """;

            // Un correo por administrador: nadie ve las direcciones de los demás.
            foreach (var destino in destinos)
                await Enviar(configuracion, destino, "Aviso: productos con stock bajo", html);
        }

        // ---------------------------------------------------------------
        // Facturas y pagos
        // ---------------------------------------------------------------

        private static readonly CultureInfo CostaRica = CultureInfo.GetCultureInfo("es-CR");

        private static string Colones(decimal monto) => monto.ToString("C", CostaRica);

        /// <summary>Costa Rica es UTC-6 todo el año (sin horario de verano).</summary>
        private static string HoraCostaRica(DateTime utc) =>
            DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(-6).ToString("dd/MM/yyyy HH:mm", CostaRica);

        private static string Html(string? texto) => WebUtility.HtmlEncode(texto ?? "");

        /// <summary>Comprobante de la compra recién registrada, con los datos para pagar.</summary>
        public Task EnviarCompraRegistrada(string correoDestino, string nombre, FacturaDetalleResponse factura)
        {
            var configuracion = ObtenerConfiguracion();
            var enlace = $"{configuracion.WebBaseUrl}/Facturas/Detalle/{factura.id}";
            var i = factura.instrucciones;

            var filas = string.Concat(factura.lineas.Select(l =>
                $"<tr><td style=\"padding:6px 12px\">{Html(l.nombre)}</td>" +
                $"<td style=\"padding:6px 12px;text-align:center\">{l.cantidad}</td>" +
                $"<td style=\"padding:6px 12px;text-align:right\">{Colones(l.precio_unitario)}</td>" +
                $"<td style=\"padding:6px 12px;text-align:right\">{Colones(l.subtotal)}</td></tr>"));

            var cedula = string.IsNullOrWhiteSpace(i.cedulaJuridica) ? "" : $" (cédula jurídica {Html(i.cedulaJuridica)})";

            return Enviar(configuracion, correoDestino, $"Comprobante de compra #{factura.id} - STAT Academy", $"""
                <h2>¡Gracias por tu compra, {Html(nombre)}!</h2>

                <p>
                    Registramos tu compra <strong>#{factura.id}</strong>
                    el {HoraCostaRica(factura.fecha)} (hora de Costa Rica).
                </p>

                <table style="border-collapse:collapse;border:1px solid #dce5f2">
                    <thead>
                        <tr style="background:#f4f7fb">
                            <th style="padding:6px 12px;text-align:left">Detalle</th>
                            <th style="padding:6px 12px">Cantidad</th>
                            <th style="padding:6px 12px;text-align:right">Precio</th>
                            <th style="padding:6px 12px;text-align:right">Subtotal</th>
                        </tr>
                    </thead>
                    <tbody>{filas}</tbody>
                    <tfoot>
                        <tr>
                            <td colspan="3" style="padding:6px 12px;text-align:right"><strong>Total</strong></td>
                            <td style="padding:6px 12px;text-align:right"><strong>{Colones(factura.total)}</strong></td>
                        </tr>
                    </tfoot>
                </table>

                <h3>Cómo pagar</h3>
                <p>Realiza el pago por <strong>{Colones(factura.total)}</strong> con una de estas opciones:</p>
                <ul>
                    <li><strong>SINPE Móvil:</strong> {Html(i.sinpeTelefono)} ({Html(i.sinpeNombre)})</li>
                    <li>
                        <strong>Transferencia o depósito:</strong> {Html(i.banco)},
                        cuenta IBAN {Html(i.cuentaIban)}, a nombre de {Html(i.cuentaTitular)}{cedula}
                    </li>
                </ul>
                <p>Indica en el detalle de la transferencia: <strong>Compra #{factura.id}</strong>.</p>

                <p>
                    Después, sube el comprobante en
                    <a href="{enlace}">Mis facturas y pagos</a>.
                    Te avisaremos por correo cuando lo verifiquemos.
                </p>
                """);
        }

        /// <summary>Aviso a los administradores: llegó un comprobante para revisar.</summary>
        public async Task EnviarComprobanteRecibido(
            IReadOnlyCollection<string> destinos, int facturaId, string usuarioNombre, decimal monto, string metodo)
        {
            var configuracion = ObtenerConfiguracion();
            var enlace = $"{configuracion.WebBaseUrl}/Admin/Invoice/{facturaId}";
            var metodoTexto = metodo == "SINPE" ? "SINPE Móvil" : "transferencia bancaria";

            var html = $"""
                <h2>Nuevo comprobante de pago por verificar</h2>

                <p>
                    {Html(usuarioNombre)} envió un comprobante por <strong>{Colones(monto)}</strong>
                    ({metodoTexto}) para la compra <strong>#{facturaId}</strong>.
                </p>

                <p><a href="{enlace}">Revisar el comprobante</a></p>
                """;

            // Un correo por administrador: nadie ve las direcciones de los demás.
            foreach (var destino in destinos)
                await Enviar(configuracion, destino, $"Comprobante por verificar - compra #{facturaId}", html);
        }

        public Task EnviarPagoAprobado(
            string correoDestino, string nombre, int facturaId, decimal monto, IReadOnlyCollection<string> cursos)
        {
            var configuracion = ObtenerConfiguracion();
            var enlace = $"{configuracion.WebBaseUrl}/Facturas/Detalle/{facturaId}";
            var enlaceCursos = $"{configuracion.WebBaseUrl}/Curso/MisCursos";

            var bloqueCursos = cursos.Count == 0
                ? ""
                : "<p>Ya tienes acceso a:</p><ul>"
                  + string.Concat(cursos.Select(c => $"<li>{Html(c)}</li>"))
                  + $"</ul><p><a href=\"{enlaceCursos}\">Ir a Mis cursos</a></p>";

            return Enviar(configuracion, correoDestino, $"Pago aprobado - compra #{facturaId}", $"""
                <h2>¡Tu pago fue aprobado, {Html(nombre)}!</h2>

                <p>
                    Confirmamos el pago de <strong>{Colones(monto)}</strong>
                    de tu compra <strong>#{facturaId}</strong>.
                </p>

                {bloqueCursos}

                <p><a href="{enlace}">Ver la factura</a></p>
                """);
        }

        public Task EnviarPagoRechazado(string correoDestino, string nombre, int facturaId, string observacion)
        {
            var configuracion = ObtenerConfiguracion();
            var enlace = $"{configuracion.WebBaseUrl}/Facturas/Detalle/{facturaId}";

            return Enviar(configuracion, correoDestino, $"No pudimos aprobar tu pago - compra #{facturaId}", $"""
                <h2>Hola, {Html(nombre)}</h2>

                <p>
                    Revisamos el comprobante que enviaste para la compra
                    <strong>#{facturaId}</strong> y <strong>no fue aprobado</strong>.
                </p>

                <p><strong>Motivo:</strong> {Html(observacion)}</p>

                <p>
                    Puedes enviar un nuevo comprobante desde
                    <a href="{enlace}">Mis facturas y pagos</a>.
                    Si crees que es un error, responde a este correo.
                </p>
                """);
        }

        private async Task Enviar(
            ConfiguracionCorreo configuracion,
            string destino,
            string asunto,
            string html)
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress("STAT Academy", configuracion.Remitente));
            mensaje.To.Add(MailboxAddress.Parse(destino));
            mensaje.Subject = asunto;
            mensaje.Body = new BodyBuilder { HtmlBody = html }.ToMessageBody();

            using var cliente = new SmtpClient { Timeout = 20_000 };

            try
            {
                // 465 = TLS implícito; 587 (y otros) = STARTTLS obligatorio.
                var seguridad = configuracion.Puerto == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

                await cliente.ConnectAsync(configuracion.Servidor, configuracion.Puerto, seguridad);
                await cliente.AuthenticateAsync(configuracion.Usuario, configuracion.Clave);
                await cliente.SendAsync(mensaje);
                await cliente.DisconnectAsync(quit: true);
            }
            catch (Exception ex) when (ex is AuthenticationException
                                       // Gmail corta la conexión con "5.7.8 ... BadCredentials"
                                       || ex.Message.Contains("5.7.8"))
            {
                // Gmail: la contraseña normal de la cuenta NO sirve; hace falta una
                // "contraseña de aplicación" (16 caracteres) con verificación en 2 pasos.
                throw new InvalidOperationException(
                    $"El servidor SMTP {configuracion.Servidor} rechazó el usuario o la clave. " +
                    "Si es Gmail, use una contraseña de aplicación en Correo:Clave.", ex);
            }
            catch (Exception ex) when (ex is SmtpCommandException or SmtpProtocolException
                                           or SslHandshakeException or IOException
                                           or System.Net.Sockets.SocketException)
            {
                throw new InvalidOperationException(
                    $"No se pudo enviar el correo por {configuracion.Servidor}:{configuracion.Puerto}: {ex.Message}", ex);
            }

            _logger.LogInformation("Correo '{Asunto}' enviado.", asunto);
        }

        private ConfiguracionCorreo ObtenerConfiguracion()
        {
            var servidor = _configuration["Correo:Servidor"];
            var puertoTexto = _configuration["Correo:Puerto"];
            var usuario = _configuration["Correo:Usuario"];
            var clave = _configuration["Correo:Clave"];
            var remitente = _configuration["Correo:Remitente"];
            var webBaseUrl = _configuration["Web:BaseUrl"];

            if (string.IsNullOrWhiteSpace(servidor)
                || string.IsNullOrWhiteSpace(puertoTexto)
                || string.IsNullOrWhiteSpace(usuario)
                || string.IsNullOrWhiteSpace(clave)
                || string.IsNullOrWhiteSpace(remitente)
                || string.IsNullOrWhiteSpace(webBaseUrl))
            {
                throw new InvalidOperationException(
                    "La configuración del correo está incompleta."
                );
            }

            if (!int.TryParse(puertoTexto, out var puerto))
            {
                throw new InvalidOperationException(
                    "El puerto del correo no es válido."
                );
            }

            return new ConfiguracionCorreo(
                servidor,
                puerto,
                usuario,
                // Gmail muestra la contraseña de aplicación con espacios ("abcd efgh ..."):
                // se quitan para que funcione igual si se pegó así.
                clave.Replace(" ", string.Empty),
                remitente,
                webBaseUrl.TrimEnd('/'));
        }

        private sealed record ConfiguracionCorreo(
            string Servidor,
            int Puerto,
            string Usuario,
            string Clave,
            string Remitente,
            string WebBaseUrl);
    }
}
