using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
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

        public async Task EnviarAlertaError(IReadOnlyCollection<string> destinos, Errores.AvisoError aviso)
        {
            var configuracion = ObtenerConfiguracion();

            // El mensaje de la excepción es solo para administradores y se recorta.
            var mensaje = aviso.Mensaje.Length > 500 ? aviso.Mensaje[..500] + "…" : aviso.Mensaje;
            var repeticiones = aviso.Repeticiones > 0
                ? $"<p>Este error se repitió <strong>{aviso.Repeticiones}</strong> vez/veces más desde el aviso anterior (no se enviaron correos para no saturar el buzón).</p>"
                : "";

            string Fila(string etiqueta, string valor) =>
                $"<tr><th style=\"padding:6px 12px;text-align:left;background:#f4f7fb\">{etiqueta}</th>" +
                $"<td style=\"padding:6px 12px\">{WebUtility.HtmlEncode(valor)}</td></tr>";

            var html = $"""
                <h2>Error del sistema en STAT Academy ({WebUtility.HtmlEncode(aviso.Origen)})</h2>

                <p>Ocurrió un error no controlado. El usuario vio una página de error genérica.</p>

                <table style="border-collapse:collapse;border:1px solid #dce5f2">
                    {Fila("Fecha (UTC)", aviso.FechaUtc.ToString("dd/MM/yyyy HH:mm:ss"))}
                    {Fila("Origen", aviso.Origen)}
                    {Fila("Ruta", aviso.Ruta)}
                    {Fila("Tipo", aviso.Tipo)}
                    {Fila("Mensaje", mensaje)}
                    {Fila("Código del error", aviso.TraceId)}
                </table>

                {repeticiones}

                <p>Busque el código del error en los registros (logs) para ver el detalle completo.</p>
                """;

            var tipoCorto = aviso.Tipo.Split('.').Last();
            foreach (var destino in destinos)
                await Enviar(configuracion, destino, $"Error del sistema ({aviso.Origen}): {tipoCorto}", html);
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
