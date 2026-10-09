using HDC.API.Infraestructura;
using HDC.BLL.Comun;
using HDC.BLL.Servicios;
using HDC.Entity.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HDC.API.Controllers;

[Route("api/domicilios"), Authorize(Policy = Permisos.GestionarDomicilios)]
public class DomiciliosController(DomicilioServicio domicilios) : ControladorBase
{
    /// <summary>RFU07</summary>
    [HttpGet]
    public async Task<List<DomicilioDto>> Listar([FromQuery] bool incluirBajas = false) => await domicilios.ListarAsync(IdUsuario, incluirBajas);

    /// <summary>RFU08</summary>
    [HttpGet("{id:long}")]
    public async Task<DomicilioDto> Obtener(long id) => await domicilios.ObtenerAsync(IdUsuario, id);

    /// <summary>RFU06</summary>
    [HttpPost]
    public async Task<DomicilioDto> Crear(DomicilioRequest req) => await domicilios.CrearAsync(IdUsuario, req);

    /// <summary>RFU09</summary>
    [HttpPut("{id:long}")]
    public async Task<DomicilioDto> Editar(long id, DomicilioRequest req) => await domicilios.ActualizarAsync(IdUsuario, id, req);

    /// <summary>RFU10 - eliminarRegistros=true borra también el historial; false lo conserva (baja lógica).</summary>
    [HttpDelete("{id:long}")]
    public async Task<MensajeDto> Eliminar(long id, [FromQuery] bool eliminarRegistros = false)
    {
        await domicilios.EliminarAsync(IdUsuario, id, eliminarRegistros);
        return new MensajeDto(eliminarRegistros
            ? "Domicilio y registros eliminados"
            : "Domicilio dado de baja; su historial de emisiones se conserva");
    }
}

[Route("api/registros"), Authorize(Policy = Politicas.CargarEmisiones)]
public class RegistrosController(RegistroServicio registros) : ControladorBase
{
    /// <summary>RFR05 - Registros propios y cargados como colaborador.</summary>
    [HttpGet]
    public async Task<List<RegistroListaDto>> Listar([FromQuery] long? idDomicilio) => await registros.ListarAsync(IdUsuario, idDomicilio);

    /// <summary>RFR06</summary>
    [HttpGet("{id:long}")]
    public async Task<RegistroDetalleDto> Obtener(long id) => await registros.ObtenerAsync(IdUsuario, id);

    /// <summary>RFR01</summary>
    [HttpPost]
    public async Task<RegistroDetalleDto> Crear(RegistroRequest req) => await registros.CrearAsync(IdUsuario, req);

    /// <summary>RFR03</summary>
    [HttpPut("{id:long}")]
    public async Task<RegistroDetalleDto> Editar(long id, RegistroRequest req) => await registros.ActualizarAsync(IdUsuario, id, req);

    /// <summary>RFR04</summary>
    [HttpDelete("{id:long}")]
    public async Task<MensajeDto> Eliminar(long id)
    {
        await registros.EliminarAsync(IdUsuario, id);
        return new MensajeDto("Registro eliminado");
    }
}

[Route("api/colaboraciones"), Authorize(Policy = Politicas.Colaboraciones)]
public class ColaboracionesController(ColaboracionServicio colaboraciones) : ControladorBase
{
    [HttpGet("enviadas"), Authorize(Policy = Permisos.GestionarColaboraciones)]
    public async Task<List<ColaboracionDto>> Enviadas() => await colaboraciones.ListarEnviadasAsync(IdUsuario);

    [HttpGet("recibidas"), Authorize(Policy = Permisos.Colaborar)]
    public async Task<List<ColaboracionDto>> Recibidas() => await colaboraciones.ListarRecibidasAsync(IdUsuario);

    /// <summary>RFU03 - Agregar un colaborador.</summary>
    [HttpPost, Authorize(Policy = Permisos.GestionarColaboraciones)]
    public async Task<ColaboracionDto> Invitar(InvitarColaboradorRequest req) => await colaboraciones.InvitarAsync(IdUsuario, req);

    [HttpPut("{id:long}"), Authorize(Policy = Permisos.GestionarColaboraciones)]
    public async Task<ColaboracionDto> Editar(long id, ActualizarColaboracionRequest req) => await colaboraciones.ActualizarAsync(IdUsuario, id, req);

    [HttpPost("{id:long}/aceptar"), Authorize(Policy = Permisos.Colaborar)]
    public async Task<ColaboracionDto> Aceptar(long id) => await colaboraciones.AceptarAsync(IdUsuario, id);

    [HttpPost("{id:long}/rechazar"), Authorize(Policy = Permisos.Colaborar)]
    public async Task<ColaboracionDto> Rechazar(long id) => await colaboraciones.RechazarAsync(IdUsuario, id);

    [HttpPost("{id:long}/revocar")]
    public async Task<ColaboracionDto> Revocar(long id) => await colaboraciones.RevocarAsync(IdUsuario, id);
}

[Route("api/dashboard"), Authorize(Policy = Permisos.VerDashboard)]
public class DashboardController(EstadisticaServicio estadisticas) : ControladorBase
{
    /// <summary>RFU11 - Estadísticas de las emisiones de los domicilios propios.</summary>
    [HttpGet]
    public async Task<DashboardDto> Obtener([FromQuery] long? idDomicilio, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta) =>
        await estadisticas.DashboardAsync(IdUsuario, idDomicilio, desde, hasta);
}

[Route("api/reportes")]
public class ReportesController(EstadisticaServicio estadisticas) : ControladorBase
{
    /// <summary>RFR02 - Reporte sobre las emisiones de los domicilios propios.</summary>
    [HttpPost("generar"), Authorize(Policy = Permisos.GenerarReportes)]
    public async Task<ReporteResultadoDto> Generar(FiltroEmisiones filtros) =>
        await estadisticas.GenerarReporteAsync(IdUsuario, filtros, global: false);

    /// <summary>Reporte del administrador sobre todos los usuarios (tipo de usuario, zona, período...).</summary>
    [HttpPost("global/generar"), Authorize(Policy = Permisos.ReportesGlobales)]
    public async Task<ReporteResultadoDto> GenerarGlobal(FiltroEmisiones filtros) =>
        await estadisticas.GenerarReporteAsync(IdUsuario, filtros, global: true);

    [HttpGet("guardados"), Authorize(Policy = Permisos.GenerarReportes)]
    public async Task<List<ReporteGuardadoDto>> Guardados([FromQuery] bool global = false) =>
        await estadisticas.ListarGuardadosAsync(IdUsuario, global);

    [HttpPost("guardados"), Authorize(Policy = Permisos.GenerarReportes)]
    public async Task<ReporteGuardadoDto> Guardar(GuardarReporteRequest req)
    {
        if (req.EsGlobal && !User.HasClaim(Permisos.Claim, Permisos.ReportesGlobales))
            throw new ProhibidoException();
        return await estadisticas.GuardarAsync(IdUsuario, req);
    }

    [HttpDelete("guardados/{id:long}"), Authorize(Policy = Permisos.GenerarReportes)]
    public async Task<MensajeDto> EliminarGuardado(long id)
    {
        await estadisticas.EliminarGuardadoAsync(IdUsuario, id);
        return new MensajeDto("Reporte eliminado");
    }
}
