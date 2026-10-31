using System.Threading.Channels;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace HDC.BLL.Servicios;

/// <summary>Un email listo para enviar (HTML + texto plano para clientes de correo sin HTML).</summary>
public record MensajeEmail(string Para, string Asunto, string Html, string TextoPlano);

/// <summary>
/// Cola en memoria de emails pendientes. Los servicios solo ENCOLAN y responden al instante;
/// el envío real lo hace <see cref="EnvioEmailsServicio"/> en segundo plano.
/// Ventajas: la respuesta HTTP no espera al servidor SMTP (que puede tardar segundos) y el tiempo de
/// respuesta de "Olvidé mi contraseña" es el mismo exista o no el email (no revela qué cuentas existen).
/// Singleton: la comparten todos los requests.
/// </summary>
public class ColaEmails
{
    // Acotada: si el SMTP se cae y se acumulan 500 emails, los nuevos se descartan en vez de llenar la memoria
    private readonly Channel<MensajeEmail> canal = Channel.CreateBounded<MensajeEmail>(
        new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropWrite });

    public bool Encolar(MensajeEmail mensaje) => canal.Writer.TryWrite(mensaje);

    internal ChannelReader<MensajeEmail> Lector => canal.Reader;
}

/// <summary>
/// Servicio en segundo plano que toma los emails de la cola y los envía por SMTP con MailKit.
/// Configuración (sección "Email" de appsettings o variables Email__* del docker-compose):
///   Host, Puerto, Usuario, Contrasena, Seguridad (Ninguna | StartTls | SslOnConnect | Auto),
///   Remitente, NombreRemitente.
/// El proveedor SMTP se cambia solo desde la configuración, sin tocar código.
/// </summary>
public class EnvioEmailsServicio(ColaEmails cola, IConfiguration config, ILogger<EnvioEmailsServicio> logger) : BackgroundService
{
    private const int Intentos = 3;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var mensaje in cola.Lector.ReadAllAsync(ct))
        {
            for (var intento = 1; intento <= Intentos; intento++)
            {
                try
                {
                    await EnviarAsync(mensaje, ct);
                    logger.LogInformation("Email \"{Asunto}\" enviado a {Para}", mensaje.Asunto, mensaje.Para);
                    break;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "No se pudo enviar el email a {Para} (intento {Intento}/{Total})", mensaje.Para, intento, Intentos);
                    // Espera creciente entre intentos: 5 s, 10 s
                    if (intento < Intentos) await Task.Delay(TimeSpan.FromSeconds(5 * intento), ct);
                }
            }
        }
    }

    private async Task EnviarAsync(MensajeEmail m, CancellationToken ct)
    {
        var s = config.GetSection("Email");
        var host = s["Host"];
        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("Falta configurar Email:Host (servidor SMTP)");

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(s["NombreRemitente"] ?? "Huella de Carbono", s["Remitente"] ?? "no-responder@hdc.local"));
        email.To.Add(MailboxAddress.Parse(m.Para));
        email.Subject = m.Asunto;
        email.Body = new BodyBuilder { HtmlBody = m.Html, TextBody = m.TextoPlano }.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, s.GetValue("Puerto", 587), Seguridad(s["Seguridad"]), ct);
        // Mailpit (desarrollo) no pide usuario; un proveedor real sí
        if (!string.IsNullOrWhiteSpace(s["Usuario"]))
            await smtp.AuthenticateAsync(s["Usuario"], s["Contrasena"], ct);
        await smtp.SendAsync(email, ct);
        await smtp.DisconnectAsync(true, ct);
    }

    private static SecureSocketOptions Seguridad(string? valor) => valor?.Trim().ToLowerInvariant() switch
    {
        "ninguna" => SecureSocketOptions.None,           // sin cifrar: solo para Mailpit en la red interna de Docker
        "starttls" => SecureSocketOptions.StartTls,      // puerto 587 (lo más común en proveedores)
        "sslonconnect" => SecureSocketOptions.SslOnConnect, // puerto 465
        _ => SecureSocketOptions.Auto
    };
}
