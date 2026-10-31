using System.Net;
using System.Security.Cryptography;
using System.Text;
using HDC.BLL.Comun;
using HDC.DAL.Repositorios;
using HDC.Entity.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HDC.BLL.Servicios;

/// <summary>
/// Recuperación de contraseña ("Olvidé mi contraseña").
///
/// Flujo:
///   1. SolicitarAsync: el usuario escribe su email. Si la cuenta existe y está activa se genera un
///      token aleatorio, se guarda SOLO su hash y se envía por email un link con el token.
///   2. RestablecerAsync: el usuario abre el link, escribe la nueva contraseña y el frontend manda
///      token + contraseña. Si el token es válido, no venció y no se usó, se cambia la contraseña.
///
/// Medidas de seguridad:
///   - Token de 256 bits generado con un generador criptográfico (imposible de adivinar).
///   - En la base se guarda el hash SHA-256 del token, nunca el token (si se filtra la tabla, los links no sirven).
///   - Vence a los Recuperacion:MinutosValidez minutos (30 por defecto) y sirve UNA sola vez.
///   - Al pedir un link nuevo, o al cambiar la contraseña, se invalidan los links anteriores.
///   - La respuesta del paso 1 es siempre la misma, exista o no el email: no revela qué cuentas existen.
///   - El endpoint tiene límite de pedidos por IP (ver Program.cs, política "recuperacion").
///   - Al cambiar la contraseña se avisa por email al usuario (si no fue él, se entera).
/// </summary>
public class RecuperacionServicio(
    UsuarioRepositorio usuarios,
    RecuperacionRepositorio recuperaciones,
    ColaEmails emails,
    IConfiguration config,
    ILogger<RecuperacionServicio> logger)
{
    /// <summary>Mensaje único del paso 1 (igual exista o no la cuenta).</summary>
    public const string MensajeSolicitud =
        "Si el email corresponde a una cuenta activa, te enviamos un link para crear una nueva contraseña. Revisá tu bandeja de entrada y la carpeta de spam.";

    private int MinutosValidez => Math.Clamp(config.GetValue("Recuperacion:MinutosValidez", 30), 5, 24 * 60);

    public async Task SolicitarAsync(string emailIngresado)
    {
        var email = emailIngresado.Trim().ToLowerInvariant();
        var u = await usuarios.ObtenerPorEmailAsync(email);
        if (u is null || u.FechaBaja != null)
        {
            // No se informa al usuario (ver MensajeSolicitud); solo queda en el log del servidor
            logger.LogInformation("Recuperación pedida para un email inexistente o deshabilitado");
            return;
        }

        var ahora = DateTime.Now;
        // Un solo link vigente por usuario: los anteriores se marcan como usados
        foreach (var anterior in await recuperaciones.ListarPendientesAsync(u.IdUsuario))
            anterior.FechaUso = ahora;

        var token = GenerarToken();
        recuperaciones.Agregar(new RecuperacionContrasena
        {
            IdUsuario = u.IdUsuario,
            TokenHash = Hash(token),
            FechaCreacion = ahora,
            FechaExpiracion = ahora.AddMinutes(MinutosValidez)
        });
        await recuperaciones.GuardarAsync();

        var link = $"{UrlFrontend()}/restablecer-contrasena?token={token}";
        emails.Encolar(EmailLink(u.Email, Mapeos.NombreMostrar(u), link, MinutosValidez));
    }

    public async Task RestablecerAsync(string token, string contrasenaNueva)
    {
        const string invalido = "El link no es válido o ya venció. Pedí uno nuevo desde \"¿Olvidaste tu contraseña?\".";

        var r = await recuperaciones.ObtenerPorHashAsync(Hash(token.Trim().ToLowerInvariant()));
        var ahora = DateTime.Now;
        if (r is null || r.FechaUso != null || r.FechaExpiracion < ahora || r.Usuario.FechaBaja != null)
            throw new NegocioException(invalido);

        r.Usuario.Contrasena = AuthServicio.HashContrasena(contrasenaNueva);
        // Se invalida este link y cualquier otro pendiente del mismo usuario
        foreach (var pendiente in await recuperaciones.ListarPendientesAsync(r.IdUsuario))
            pendiente.FechaUso = ahora;
        r.FechaUso = ahora;
        await recuperaciones.GuardarAsync();

        emails.Encolar(EmailConfirmacion(r.Usuario.Email));
        logger.LogInformation("Contraseña restablecida para el usuario {IdUsuario}", r.IdUsuario);
    }

    /// <summary>32 bytes aleatorios (256 bits) en hexadecimal: 64 caracteres, seguros para usar en una URL.</summary>
    private static string GenerarToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    /// <summary>SHA-256 del token en hexadecimal (64 caracteres): es lo único que se guarda en la base.</summary>
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private string UrlFrontend() => (config["Recuperacion:UrlFrontend"] ?? "http://localhost:8081").TrimEnd('/');

    // ---------------- Plantillas de email ----------------

    private static MensajeEmail EmailLink(string para, string nombre, string link, int minutos)
    {
        var n = WebUtility.HtmlEncode(nombre);
        var html = Plantilla("Recuperá tu contraseña", $"""
            <p>Hola {n}:</p>
            <p>Recibimos un pedido para restablecer la contraseña de tu cuenta en <b>Huella de Carbono</b>.</p>
            <p style="text-align:center;margin:28px 0">
              <a href="{link}" style="background:#2e7d32;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:bold">Crear nueva contraseña</a>
            </p>
            <p>El link vence en <b>{minutos} minutos</b> y se puede usar una sola vez.</p>
            <p style="color:#666;font-size:13px">Si el botón no funciona, copiá esta dirección en el navegador:<br><span style="word-break:break-all">{link}</span></p>
            <p style="color:#666;font-size:13px">Si no fuiste vos, ignorá este email: tu contraseña no cambia.</p>
            """);
        var texto = $"Hola {nombre}:\n\nPara crear una nueva contraseña en Huella de Carbono abrí este link (vence en {minutos} minutos):\n{link}\n\nSi no fuiste vos, ignorá este email.";
        return new MensajeEmail(para, "Recuperá tu contraseña - Huella de Carbono", html, texto);
    }

    private static MensajeEmail EmailConfirmacion(string para)
    {
        var html = Plantilla("Tu contraseña fue cambiada", """
            <p>La contraseña de tu cuenta en <b>Huella de Carbono</b> se cambió correctamente.</p>
            <p style="color:#666;font-size:13px">Si no fuiste vos, pedí un nuevo link desde "¿Olvidaste tu contraseña?" y avisá al administrador.</p>
            """);
        return new MensajeEmail(para, "Tu contraseña fue cambiada - Huella de Carbono", html,
            "La contraseña de tu cuenta en Huella de Carbono se cambió correctamente. Si no fuiste vos, avisá al administrador.");
    }

    /// <summary>Estructura común de los emails (estilos en línea: muchos clientes de correo ignoran &lt;style&gt;).</summary>
    private static string Plantilla(string titulo, string cuerpo) => $"""
        <div style="font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:0 auto;color:#222">
          <div style="background:#2e7d32;color:#fff;padding:16px 24px;border-radius:8px 8px 0 0">
            <h2 style="margin:0;font-size:20px">{titulo}</h2>
          </div>
          <div style="border:1px solid #ddd;border-top:0;padding:16px 24px;border-radius:0 0 8px 8px;line-height:1.5">
            {cuerpo}
          </div>
        </div>
        """;
}
