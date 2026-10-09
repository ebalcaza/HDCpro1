using HDC.BLL.Comun;
using HDC.DAL.Repositorios;
using HDC.Entity.Dtos;
using HDC.Entity.Entities;

namespace HDC.BLL.Servicios;

/// <summary>
/// Flujo: el titular invita (PENDIENTE) → el invitado acepta (ACEPTADA) o rechaza (RECHAZADA).
/// Cualquiera de los dos puede revocarla (REVOCADA). Mientras está aceptada y el período la cubre,
/// el colaborador carga consumos de los tipos de emisión habilitados, que suman al domicilio del titular.
/// </summary>
public class ColaboracionServicio(
    ColaboracionRepositorio colaboraciones,
    DomicilioRepositorio domicilios,
    UsuarioRepositorio usuarios,
    TipoEmisionRepositorio tipos)
{
    public async Task<List<ColaboracionDto>> ListarEnviadasAsync(long idTitular) =>
        await ADtosAsync(await colaboraciones.ListarPorTitularAsync(idTitular));

    public async Task<List<ColaboracionDto>> ListarRecibidasAsync(long idColaborador) =>
        await ADtosAsync(await colaboraciones.ListarPorColaboradorAsync(idColaborador));

    public async Task<ColaboracionDto> InvitarAsync(long idTitular, InvitarColaboradorRequest req)
    {
        var domicilio = await domicilios.ObtenerPorIdAsync(req.IdDomicilio) ?? throw new NoEncontradoException("Domicilio no encontrado");
        if (domicilio.IdUsuario != idTitular) throw new ProhibidoException("El domicilio no te pertenece");
        if (domicilio.FechaBaja != null) throw new NegocioException("El domicilio está dado de baja");

        var colaborador = await usuarios.ObtenerPorEmailAsync(req.EmailColaborador.Trim().ToLowerInvariant())
            ?? throw new NoEncontradoException("No hay ningún usuario registrado con ese email");
        if (colaborador.IdUsuario == idTitular) throw new NegocioException("No podés invitarte a vos mismo");
        if (colaborador.FechaBaja != null) throw new NegocioException("El usuario invitado tiene la cuenta deshabilitada");
        if (colaborador.Rol.RolPermisos.All(rp => rp.Permiso.Nombre != Permisos.Colaborar))
            throw new NegocioException("El usuario invitado no tiene permitido colaborar");
        if (await colaboraciones.ExisteActivaAsync(colaborador.IdUsuario, req.IdDomicilio))
            throw new ConflictoException("Ese usuario ya tiene una invitación pendiente o aceptada en este domicilio");

        var c = new Colaboracion
        {
            IdDomicilio = req.IdDomicilio,
            IdUsuarioColaborador = colaborador.IdUsuario,
            Estado = EstadoColaboracion.Pendiente,
            Mensaje = Mapeos.Limpiar(req.Mensaje),
            FechaInvitacion = DateTime.Now
        };
        await AplicarAsync(c, req.PeriodoDesde, req.PeriodoHasta, req.IdsTiposEmision);
        colaboraciones.Agregar(c);
        await colaboraciones.GuardarAsync();
        return await ObtenerDtoAsync(c.IdColaboracion);
    }

    public async Task<ColaboracionDto> ActualizarAsync(long idTitular, long id, ActualizarColaboracionRequest req)
    {
        var c = await ObtenerAsync(id);
        if (c.Domicilio.IdUsuario != idTitular) throw new ProhibidoException();
        if (c.Estado is not (EstadoColaboracion.Pendiente or EstadoColaboracion.Aceptada))
            throw new NegocioException("Solo se pueden modificar colaboraciones pendientes o aceptadas");
        await AplicarAsync(c, req.PeriodoDesde, req.PeriodoHasta, req.IdsTiposEmision);
        await colaboraciones.GuardarAsync();
        return await ObtenerDtoAsync(id);
    }

    public Task<ColaboracionDto> AceptarAsync(long idColaborador, long id) => ResponderAsync(idColaborador, id, EstadoColaboracion.Aceptada);

    public Task<ColaboracionDto> RechazarAsync(long idColaborador, long id) => ResponderAsync(idColaborador, id, EstadoColaboracion.Rechazada);

    public async Task<ColaboracionDto> RevocarAsync(long idUsuario, long id)
    {
        var c = await ObtenerAsync(id);
        if (c.Domicilio.IdUsuario != idUsuario && c.IdUsuarioColaborador != idUsuario) throw new ProhibidoException();
        if (c.Estado is not (EstadoColaboracion.Pendiente or EstadoColaboracion.Aceptada))
            throw new NegocioException("La colaboración ya no está activa");
        c.Estado = EstadoColaboracion.Revocada;
        c.FechaRespuesta = DateTime.Now;
        await colaboraciones.GuardarAsync();
        return await ObtenerDtoAsync(id);
    }

    private async Task<ColaboracionDto> ResponderAsync(long idColaborador, long id, string estado)
    {
        var c = await ObtenerAsync(id);
        if (c.IdUsuarioColaborador != idColaborador) throw new ProhibidoException("La invitación no es para vos");
        if (c.Estado != EstadoColaboracion.Pendiente) throw new NegocioException("La invitación ya fue respondida");
        c.Estado = estado;
        c.FechaRespuesta = DateTime.Now;
        await colaboraciones.GuardarAsync();
        return await ObtenerDtoAsync(id);
    }

    private async Task AplicarAsync(Colaboracion c, DateOnly desde, DateOnly? hasta, List<long> idsTipos)
    {
        var d = Mapeos.InicioDeMes(desde);
        var h = hasta is DateOnly x ? Mapeos.InicioDeMes(x) : (DateOnly?)null;
        if (h < d) throw new NegocioException("El período hasta debe ser posterior al período desde");

        var ids = idsTipos.Distinct().ToList();
        var encontrados = await tipos.ObtenerPorIdsAsync(ids);
        if (encontrados.Count != ids.Count) throw new NegocioException("Alguno de los tipos de emisión no existe");

        c.PeriodoDesde = d;
        c.PeriodoHasta = h;
        var quitar = c.TiposEmision.Where(t => !ids.Contains(t.IdTipoEmision)).ToList();
        colaboraciones.QuitarTipos(quitar);
        foreach (var q in quitar) c.TiposEmision.Remove(q);
        foreach (var id in ids.Where(id => c.TiposEmision.All(t => t.IdTipoEmision != id)))
            c.TiposEmision.Add(new ColaboracionTipoEmision { IdTipoEmision = id });
    }

    private async Task<Colaboracion> ObtenerAsync(long id) =>
        await colaboraciones.ObtenerCompletaAsync(id) ?? throw new NoEncontradoException("Colaboración no encontrada");

    private async Task<ColaboracionDto> ObtenerDtoAsync(long id) =>
        (await ADtosAsync([(await colaboraciones.ObtenerCompletaAsync(id))!]))[0];

    private async Task<List<ColaboracionDto>> ADtosAsync(List<Colaboracion> lista)
    {
        var aportes = await colaboraciones.AportesAsync(lista.Select(c => c.IdDomicilio).Distinct().ToList());
        var mes = Mapeos.MesActual();
        return lista.Select(c =>
        {
            var aporte = aportes.GetValueOrDefault((c.IdDomicilio, c.IdUsuarioColaborador));
            var vigente = c.Estado == EstadoColaboracion.Aceptada && c.PeriodoDesde <= mes && (c.PeriodoHasta == null || c.PeriodoHasta >= mes);
            return new ColaboracionDto(
                c.IdColaboracion, c.IdDomicilio, Mapeos.DescripcionCorta(c.Domicilio),
                c.Domicilio.IdUsuario, Mapeos.NombreMostrar(c.Domicilio.Usuario), c.Domicilio.Usuario.Email,
                c.IdUsuarioColaborador, Mapeos.NombreMostrar(c.Colaborador), c.Colaborador.Email,
                c.PeriodoDesde, c.PeriodoHasta, c.Estado, c.Mensaje, c.FechaInvitacion, c.FechaRespuesta, vigente,
                c.TiposEmision.Select(t => new TipoEmisionResumenDto(t.IdTipoEmision, t.TipoEmision.Nombre, t.TipoEmision.IdAlcance, t.TipoEmision.Alcance.Nombre))
                    .OrderBy(t => t.Alcance).ThenBy(t => t.Nombre).ToList(),
                aporte.Registros, aporte.Kg);
        }).ToList();
    }
}
