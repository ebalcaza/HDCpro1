using HDC.BLL.Comun;
using Microsoft.EntityFrameworkCore;
using Npgsql; // Namespace necesario para capturar excepciones específicas de PostgreSQL

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
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23503" })
        {
            // Código Postgres para violación de llave foránea (Foreign Key violation - equivalente al error 547 de SQL Server)
            await EscribirAsync(ctx, StatusCodes.Status409Conflict, "El dato está relacionado con otros registros y no puede modificarse o eliminarse");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // Código Postgres para violación de restricción única (Unique violation - equivalente a los errores 2627/2601 de SQL Server)
            await EscribirAsync(ctx, StatusCodes.Status409Conflict, "Ya existe un registro con esos datos");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "22003" })
        {
            // Código Postgres para desbordamiento numérico (Numeric value out of range - excede DECIMAL(18,6))
            await EscribirAsync(ctx, StatusCodes.Status400BadRequest, "El valor ingresado o el resultado del cálculo supera el límite máximo permitido por el sistema.");
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