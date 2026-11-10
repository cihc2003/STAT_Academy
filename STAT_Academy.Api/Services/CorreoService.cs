using System.Net;
using System.Net.Mail;

namespace STAT_Academy.Api.Services
{
    public class CorreoService
    {
        private readonly IConfiguration _configuration;

        public CorreoService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarRecuperacionContrasena(
            string correoDestino,
            string token)
        {
            var configuracion = ObtenerConfiguracion();

            var tokenCodificado = Uri.EscapeDataString(token);

            var enlace =
                $"{configuracion.webBaseUrl}" +
                "/Cuenta/RestablecerContrasena" +
                $"?token={tokenCodificado}";

            using var mensaje = new MailMessage
            {
                From = new MailAddress(
                    configuracion.remitente,
                    "STAT Academy"
                ),
                Subject = "Recuperación de contraseña",
                IsBodyHtml = true,
                Body = $"""
                    <h2>Recuperación de contraseña</h2>

                    <p>
                        Recibimos una solicitud para restablecer
                        tu contraseña de STAT Academy.
                    </p>

                    <p>
                        <a href="{enlace}">
                            Restablecer mi contraseña
                        </a>
                    </p>

                    <p>
                        Este enlace vence en 30 minutos y solamente
                        puede utilizarse una vez.
                    </p>

                    <p>
                        Si no realizaste esta solicitud,
                        puedes ignorar este mensaje.
                    </p>
                    """
            };

            mensaje.To.Add(correoDestino);

            using var clienteSmtp = CrearClienteSmtp(configuracion);

            await clienteSmtp.SendMailAsync(mensaje);
        }

        public async Task EnviarConfirmacionCambioCorreo(
            string correoNuevo,
            string token)
        {
            var configuracion = ObtenerConfiguracion();

            var tokenCodificado = Uri.EscapeDataString(token);

            var enlace =
                $"{configuracion.webBaseUrl}" +
                "/Cuenta/ConfirmarCambioCorreo" +
                $"?token={tokenCodificado}";

            using var mensaje = new MailMessage
            {
                From = new MailAddress(
                    configuracion.remitente,
                    "STAT Academy"
                ),
                Subject = "Confirme su nuevo correo electrónico",
                IsBodyHtml = true,
                Body = $"""
                    <h2>Confirmación de correo electrónico</h2>

                    <p>
                        Recibimos una solicitud para asociar esta
                        dirección de correo con una cuenta de
                        STAT Academy.
                    </p>

                    <p>
                        <a href="{enlace}">
                            Confirmar mi nuevo correo
                        </a>
                    </p>

                    <p>
                        Este enlace vence en 30 minutos.
                    </p>

                    <p>
                        Si usted no solicitó este cambio,
                        puede ignorar este mensaje.
                    </p>
                    """
            };

            mensaje.To.Add(correoNuevo);

            using var clienteSmtp = CrearClienteSmtp(configuracion);

            await clienteSmtp.SendMailAsync(mensaje);
        }

        private (
            string servidor,
            int puerto,
            string usuario,
            string clave,
            string remitente,
            string webBaseUrl
        ) ObtenerConfiguracion()
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

            return (
                servidor,
                puerto,
                usuario,
                clave,
                remitente,
                webBaseUrl
            );
        }

        private static SmtpClient CrearClienteSmtp(
            (
                string servidor,
                int puerto,
                string usuario,
                string clave,
                string remitente,
                string webBaseUrl
            ) configuracion)
        {
            return new SmtpClient(
                configuracion.servidor,
                configuracion.puerto
            )
            {
                Credentials = new NetworkCredential(
                    configuracion.usuario,
                    configuracion.clave
                ),
                EnableSsl = true
            };
        }
    }
}