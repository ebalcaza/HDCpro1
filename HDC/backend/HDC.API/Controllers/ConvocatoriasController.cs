using HDC.API.Infraestructura;
using HDC.BLL.Comun;
using HDC.BLL.Servicios;
using HDC.Entity.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HDC.API.Controllers;

/// <summary>
/// Convocatorias (v2.2): colaboración masiva por código.
/// - El titular (permiso GESTIONAR_COLABORACIONES) las crea y gestiona a sus miembros.
/// - Quien quiere colaborar (permiso COLABORAR) se suma escribiendo el código.
/// </summary>
[Route("api/convocatorias")]
public class ConvocatoriasController(ConvocatoriaServicio convocatorias) : ControladorBase
{
    // ----------------------- Titular -----------------------

    [HttpGet, Authorize(Policy = Permisos.GestionarColaboraciones)]
    public async Task<List<ConvocatoriaDto>> Listar() => await convocatorias.ListarAsync(IdUsuario);

    [HttpGet("{id:long}"), Authorize(Policy = Permisos.GestionarColaboraciones)]
    public async Task<ConvocatoriaDto> Obtener(long id) => await convocatorias.ObtenerAsync(IdUsuario, id);

    [HttpPost, Authorize(Policy = Permisos.GestionarColaboraciones)]
    public async Task<ConvocatoriaDto> Crear(CrearConvocatoriaRequest req) => await convocatorias.CrearAsync(IdUsuario, req);

    [HttpPut("{id:long}"), Authorize(Policy = Permisos.GestionarColaboraciones)]
    public async Task<ConvocatoriaDto> Editar(long id, ConvocatoriaRequest req) => await convocatorias.ActualizarAsync(IdUsuario, id, req);

    /// <summary>ABIERTA, CERRADA o FINALIZADA.</summary>
    [HttpPut("{id:long}/estado"), Authorize(Policy = Permisos.GestionarColaboraciones)]
    public async Task<ConvocatoriaDto> CambiarEstado(long id, CambiarEstadoConvocatoriaRequest req) =>
        await convocatorias.CambiarEstadoAsync(IdUsuario, id, req.Estado);

    [HttpPost("{id:long}/codigo"), Authorize(Policy = Permisos.GestionarColaboraciones)]
    public async Task<ConvocatoriaDto> RegenerarCodigo(long id) => await convocatorias.RegenerarCodigoAsync(IdUsuario, id);

    [HttpDelete("{id:long}"), Authorize(Policy = Permisos.GestionarColaboraciones)]
    public async Task<MensajeDto> Eliminar(long id)
    {
        await convocatorias.EliminarAsync(IdUsuario, id);
        return new MensajeDto("Convocatoria eliminada");
    }

    /// <summary>Miembros paginados en el servidor (?pagina, tamanio, orden, asc, busqueda, estado).</summary>
    [HttpGet("{id:long}/miembros"), Authorize(Policy = Permisos.GestionarColaboraciones)]
    public async Task<PaginaDto<MiembroConvocatoriaDto>> Miembros(long id, [FromQuery] FiltroMiembros filtro) =>
        await convocatorias.ListarMiembrosAsync(IdUsuario, id, filtro);

    /// <summary>Aceptar, rechazar, dar de baja o reactivar a varios miembros (seleccionados o todos los filtrados).</summary>
    [HttpPost("{id:long}/miembros/accion"), Authorize(Policy = Permisos.GestionarColaboraciones)]
    public async Task<ResultadoAccionDto> AccionMiembros(long id, AccionMiembrosRequest req) =>
        await convocatorias.AplicarAccionAsync(IdUsuario, id, req);

    // ----------------------- Colaborador -----------------------

    /// <summary>Ver a qué convocatoria corresponde un código antes de sumarse. Limitado a 20 intentos cada 15 min.</summary>
    [HttpPost("codigo/vista-previa"), Authorize(Policy = Permisos.Colaborar), EnableRateLimiting(LimitesPedidos.CodigoConvocatoria)]
    public async Task<VistaPreviaConvocatoriaDto> VistaPrevia(CodigoConvocatoriaRequest req) =>
        await convocatorias.VistaPreviaAsync(IdUsuario, req.Codigo);

    /// <summary>Sumarse a la convocatoria del código.</summary>
    [HttpPost("unirse"), Authorize(Policy = Permisos.Colaborar), EnableRateLimiting(LimitesPedidos.CodigoConvocatoria)]
    public async Task<ColaboracionDto> Unirse(CodigoConvocatoriaRequest req) =>
        await convocatorias.UnirseAsync(IdUsuario, req.Codigo);
}
