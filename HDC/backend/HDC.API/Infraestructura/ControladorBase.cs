using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HDC.API.Infraestructura;

public static class Politicas
{
    public const string CargarEmisiones = "CARGAR_EMISIONES";
    public const string Colaboraciones = "COLABORACIONES";
}

[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class ControladorBase : ControllerBase
{
    /// <summary>Id del usuario autenticado (claim "sub" del JWT).</summary>
    protected long IdUsuario => long.Parse(User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException("Token sin usuario"));
}
