using HDC.BLL.Comun;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HDC.API.Infraestructura;

/// <summary>Traduce las excepciones de negocio a respuestas HTTP con el formato { mensaje }.</summary>
public class ManejoErroresMiddleware(RequestDelegate next, ILogger<ManejoErroresMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (HdcException ex)
        {
            await EscribirAsync(ctx, ex switch
            {
                NoAutorizadoException => StatusCodes.Status401Unauthorized,
                ProhibidoException => StatusCodes.Status403Forbidden,
                NoEncontradoException => StatusCodes.Status404NotFound,
                ConflictoException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            }, ex.Message);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            await EscribirAsync(ctx, StatusCodes.Status409Conflict, "El dato está relacionado con otros registros y no puede modificarse o eliminarse");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2627 or 2601 })
        {
            await EscribirAsync(ctx, StatusCodes.Status409Conflict, "Ya existe un registro con esos datos");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", ctx.Request.Method, ctx.Request.Path);
            await EscribirAsync(ctx, StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado. Intentá nuevamente.");
        }
    }

    private static Task EscribirAsync(HttpContext ctx, int status, string mensaje)
    {
        if (ctx.Response.HasStarted) return Task.CompletedTask;
        ctx.Response.StatusCode = status;
        return ctx.Response.WriteAsJsonAsync(new { mensaje });
    }
}
