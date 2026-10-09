using HDC.API.Infraestructura;
using HDC.BLL.Comun;
using HDC.BLL.Servicios;
using HDC.Entity.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HDC.API.Controllers;

/// <summary>
/// Catálogos de parametrización. La lectura está disponible para cualquier usuario autenticado
/// (los formularios de emisiones los necesitan); la escritura requiere permisos de administración.
/// </summary>
[Route("api/catalogos")]
public class CatalogosController(CatalogoServicio catalogos, FuenteEmisionServicio fuentes, SemillaServicio semilla) : ControladorBase
{
    // ----- Alcances (RFA31-34) -----
    [HttpGet("alcances")]
    public async Task<List<AlcanceDto>> Alcances() => await catalogos.ListarAlcancesAsync();

    [HttpPost("alcances"), Authorize(Policy = Permisos.GestionarCatalogos)]
    public async Task<AlcanceDto> CrearAlcance(AlcanceRequest req) => await catalogos.GuardarAlcanceAsync(null, req);

    [HttpPut("alcances/{id:long}"), Authorize(Policy = Permisos.GestionarCatalogos)]
    public async Task<AlcanceDto> EditarAlcance(long id, AlcanceRequest req) => await catalogos.GuardarAlcanceAsync(id, req);

    [HttpDelete("alcances/{id:long}"), Authorize(Policy = Permisos.GestionarCatalogos)]
    public async Task<MensajeDto> EliminarAlcance(long id)
    {
        await catalogos.EliminarAlcanceAsync(id);
        return new MensajeDto("Alcance eliminado");
    }

    // ----- Unidades de medida (RFA27-30) -----
    [HttpGet("unidades")]
    public async Task<List<UnidadMedidaDto>> Unidades() => await catalogos.ListarUnidadesAsync();

    [HttpPost("unidades"), Authorize(Policy = Permisos.GestionarCatalogos)]
    public async Task<UnidadMedidaDto> CrearUnidad(UnidadMedidaRequest req) => await catalogos.GuardarUnidadAsync(null, req);

    [HttpPut("unidades/{id:long}"), Authorize(Policy = Permisos.GestionarCatalogos)]
    public async Task<UnidadMedidaDto> EditarUnidad(long id, UnidadMedidaRequest req) => await catalogos.GuardarUnidadAsync(id, req);

    [HttpDelete("unidades/{id:long}"), Authorize(Policy = Permisos.GestionarCatalogos)]
    public async Task<MensajeDto> EliminarUnidad(long id)
    {
        await catalogos.EliminarUnidadAsync(id);
        return new MensajeDto("Unidad eliminada");
    }

    // ----- Tipos de emisión (RFA23-26) -----
    [HttpGet("tipos-emision")]
    public async Task<List<TipoEmisionDto>> TiposEmision() => await catalogos.ListarTiposAsync();

    [HttpPost("tipos-emision"), Authorize(Policy = Permisos.GestionarCatalogos)]
    public async Task<TipoEmisionDto> CrearTipo(TipoEmisionRequest req) => await catalogos.GuardarTipoAsync(null, req);

    [HttpPut("tipos-emision/{id:long}"), Authorize(Policy = Permisos.GestionarCatalogos)]
    public async Task<TipoEmisionDto> EditarTipo(long id, TipoEmisionRequest req) => await catalogos.GuardarTipoAsync(id, req);

    [HttpDelete("tipos-emision/{id:long}"), Authorize(Policy = Permisos.GestionarCatalogos)]
    public async Task<MensajeDto> EliminarTipo(long id)
    {
        await catalogos.EliminarTipoAsync(id);
        return new MensajeDto("Tipo de emisión eliminado");
    }

    // ----- Fuentes de emisión (RFA01-04) -----
    [HttpGet("fuentes")]
    public async Task<List<FuenteEmisionDto>> Fuentes([FromQuery] bool incluirBajas = false) => await fuentes.ListarAsync(incluirBajas);

    [HttpPost("fuentes"), Authorize(Policy = Permisos.GestionarFuentes)]
    public async Task<FuenteEmisionDto> CrearFuente(FuenteEmisionRequest req) => await fuentes.CrearAsync(req);

    [HttpPut("fuentes/{id:long}"), Authorize(Policy = Permisos.GestionarFuentes)]
    public async Task<FuenteEmisionDto> EditarFuente(long id, FuenteEmisionRequest req) => await fuentes.ActualizarAsync(id, req);

    [HttpPost("fuentes/{id:long}/reactivar"), Authorize(Policy = Permisos.GestionarFuentes)]
    public async Task<FuenteEmisionDto> ReactivarFuente(long id) => await fuentes.ReactivarAsync(id);

    [HttpDelete("fuentes/{id:long}"), Authorize(Policy = Permisos.GestionarFuentes)]
    public async Task<MensajeDto> EliminarFuente(long id)
    {
        var fisica = await fuentes.EliminarAsync(id);
        return new MensajeDto(fisica
            ? "Fuente de emisión eliminada"
            : "La fuente tiene registros asociados: se dio de baja y se conserva en el historial");
    }

    // ----- Geografía -----
    [HttpPost("georef/sincronizar"), Authorize(Policy = Permisos.GestionarCatalogos)]
    public async Task<MensajeDto> SincronizarGeoref(CancellationToken ct)
    {
        var n = await semilla.SincronizarGeorefAsync(ct);
        return new MensajeDto(n == 0 ? "La geografía ya estaba cargada" : $"Se cargaron {n} ciudades desde georef");
    }
}

[Route("api/roles"), Authorize(Policy = Permisos.GestionarRoles)]
public class RolesController(RolServicio roles) : ControladorBase
{
    /// <summary>RFA18</summary>
    [HttpGet]
    public async Task<List<RolDto>> Listar() => await roles.ListarAsync();

    /// <summary>RFA15</summary>
    [HttpPost]
    public async Task<RolDto> Crear(RolRequest req) => await roles.CrearAsync(req);

    /// <summary>RFA16</summary>
    [HttpPut("{id:long}")]
    public async Task<RolDto> Editar(long id, RolRequest req) => await roles.ActualizarAsync(id, req);

    /// <summary>RFA17</summary>
    [HttpDelete("{id:long}")]
    public async Task<MensajeDto> Eliminar(long id)
    {
        await roles.EliminarAsync(id);
        return new MensajeDto("Tipo de usuario eliminado");
    }
}

[Route("api/permisos"), Authorize(Policy = Permisos.GestionarRoles)]
public class PermisosController(PermisoServicio permisos) : ControladorBase
{
    /// <summary>RFA22</summary>
    [HttpGet]
    public async Task<List<PermisoDto>> Listar() => await permisos.ListarAsync();

    /// <summary>RFA19</summary>
    [HttpPost]
    public async Task<PermisoDto> Crear(PermisoRequest req) => await permisos.CrearAsync(req);

    /// <summary>RFA20</summary>
    [HttpPut("{id:long}")]
    public async Task<PermisoDto> Editar(long id, PermisoRequest req) => await permisos.ActualizarAsync(id, req);

    /// <summary>RFA21</summary>
    [HttpDelete("{id:long}")]
    public async Task<MensajeDto> Eliminar(long id)
    {
        await permisos.EliminarAsync(id);
        return new MensajeDto("Permiso eliminado");
    }
}

[Route("api/usuarios"), Authorize(Policy = Permisos.GestionarUsuarios)]
public class UsuariosController(UsuarioServicio usuarios) : ControladorBase
{
    /// <summary>RFA05</summary>
    [HttpGet]
    public async Task<List<UsuarioListaDto>> Listar() => await usuarios.ListarAsync();

    /// <summary>RFA06</summary>
    [HttpGet("{id:long}")]
    public async Task<UsuarioDetalleDto> Obtener(long id) => await usuarios.ObtenerAsync(id);

    [HttpPut("{id:long}/rol"), Authorize(Policy = Permisos.GestionarRoles)]
    public async Task<MensajeDto> CambiarRol(long id, CambiarRolRequest req)
    {
        await usuarios.CambiarRolAsync(IdUsuario, id, req.IdRol);
        return new MensajeDto("Tipo de usuario actualizado. Se aplicará en su próximo inicio de sesión.");
    }

    [HttpPut("{id:long}/estado")]
    public async Task<MensajeDto> CambiarEstado(long id, CambiarEstadoRequest req)
    {
        await usuarios.CambiarEstadoAsync(IdUsuario, id, req.Activo);
        return new MensajeDto(req.Activo ? "Usuario habilitado" : "Usuario deshabilitado");
    }
}
