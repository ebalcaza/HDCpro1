using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace HDC.API.Infraestructura;

/// <summary>
/// Límite de pedidos por IP ("rate limiting") para los endpoints públicos sensibles.
/// Evita que alguien use "Olvidé mi contraseña" para inundar de emails a un usuario,
/// o pruebe tokens al azar en "restablecer".
/// </summary>
public static class LimitesPedidos
{
    public const string Recuperacion = "recuperacion";
    public const string CodigoConvocatoria = "codigo-convocatoria";

    /// <summary>Máximo de pedidos por IP en cada ventana de tiempo.</summary>
    private const int MaximoPedidos = 5;
    private static readonly TimeSpan Ventana = TimeSpan.FromMinutes(15);

    public static IServiceCollection AgregarLimitesPedidos(this IServiceCollection services)
    {
        // Detrás de nginx (docker-compose) la API ve la IP del contenedor de nginx, no la del usuario.
        // nginx manda la IP real en el encabezado X-Forwarded-For: este middleware la toma de ahí.
        // Se limpian las redes "conocidas" porque la IP de nginx dentro de Docker no es fija.
        // ForwardLimit = 1 (por defecto) usa solo el último valor, el que agregó nginx, así que
        // un cliente no puede falsear su IP mandando su propio X-Forwarded-For.
        services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.KnownNetworks.Clear();
            o.KnownProxies.Clear();
        });

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // Mismo formato { mensaje } que el resto de los errores de la API
            o.OnRejected = (ctx, ct) => new ValueTask(ctx.HttpContext.Response.WriteAsJsonAsync(
                new { mensaje = "Hiciste demasiados intentos. Esperá unos minutos y volvé a probar." }, ct));

            // Ventana fija por IP: 5 pedidos cada 15 minutos
            o.AddPolicy(Recuperacion, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = MaximoPedidos, Window = Ventana, QueueLimit = 0 }));

            // Códigos de convocatoria: 20 intentos cada 15 minutos por USUARIO (el endpoint exige sesión).
            // Alcanza de sobra para equivocarse al tipear, pero hace inútil probar códigos al azar
            // (hay ~1 billón de combinaciones).
            o.AddPolicy(CodigoConvocatoria, http => RateLimitPartition.GetFixedWindowLimiter(
                http.User.FindFirst("sub")?.Value ?? http.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = Ventana, QueueLimit = 0 }));
        });

        return services;
    }
}
