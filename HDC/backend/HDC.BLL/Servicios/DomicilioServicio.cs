using HDC.BLL.Comun;
using HDC.DAL.Repositorios;
using HDC.Entity.Dtos;
using HDC.Entity.Entities;

namespace HDC.BLL.Servicios;

public class DomicilioServicio(
    DomicilioRepositorio domicilios,
    GeoRepositorio geo,
    FuenteEmisionRepositorio fuentes,
    RegistroRepositorio registros,
    ColaboracionRepositorio colaboraciones)
{
    public async Task<List<DomicilioDto>> ListarAsync(long idUsuario, bool incluirBajas = false)
    {
        var lista = await domicilios.ListarPorUsuarioAsync(idUsuario, incluirBajas);
        var conteos = await domicilios.ConteosAsync(lista.Select(d => d.IdDomicilio).ToList());
        return lista.Select(d => Mapeos.ToDto(d, conteos[d.IdDomicilio].Registros, conteos[d.IdDomicilio].Colaboradores)).ToList();
    }

    public async Task<DomicilioDto> ObtenerAsync(long idUsuario, long idDomicilio)
    {
        var d = await ObtenerPropioAsync(idUsuario, idDomicilio);
        return await ADtoAsync(d);
    }

    public async Task<DomicilioDto> CrearAsync(long idUsuario, DomicilioRequest req)
    {
        var d = new Domicilio { IdUsuario = idUsuario, FechaAlta = DateTime.Now };
        await AplicarAsync(d, req);
        domicilios.Agregar(d);
        await domicilios.GuardarAsync();
        return await ADtoAsync((await domicilios.ObtenerCompletoAsync(d.IdDomicilio))!);
    }

    public async Task<DomicilioDto> ActualizarAsync(long idUsuario, long idDomicilio, DomicilioRequest req)
    {
        var d = await ObtenerPropioAsync(idUsuario, idDomicilio);
        if (d.FechaBaja != null) throw new NegocioException("No se puede editar un domicilio dado de baja");
        await AplicarAsync(d, req);
        await domicilios.GuardarAsync();
        return await ADtoAsync((await domicilios.ObtenerCompletoAsync(idDomicilio))!);
    }

    /// <summary>
    /// eliminarRegistros = true: borra el domicilio con sus registros y colaboraciones.
    /// eliminarRegistros = false: baja lógica, conserva el historial de emisiones.
    /// </summary>
    public async Task EliminarAsync(long idUsuario, long idDomicilio, bool eliminarRegistros)
    {
        var d = await ObtenerPropioAsync(idUsuario, idDomicilio);
        var activos = (await domicilios.ListarPorUsuarioAsync(idUsuario)).Count;
        if (d.FechaBaja == null && activos <= 1)
            throw new NegocioException("Tenés que conservar al menos un domicilio activo");

        if (eliminarRegistros)
        {
            registros.EliminarRango(await registros.ListarPorDomicilioAsync(idDomicilio));
            domicilios.Eliminar(d);
        }
        else
        {
            d.FechaBaja = DateTime.Now;
            foreach (var c in (await colaboraciones.ListarPorTitularAsync(idUsuario)).Where(c => c.IdDomicilio == idDomicilio))
            {
                if (c.Estado is EstadoColaboracion.Pendiente or EstadoColaboracion.Aceptada)
                {
                    var tracked = (await colaboraciones.ObtenerPorIdAsync(c.IdColaboracion))!;
                    tracked.Estado = EstadoColaboracion.Revocada;
                    tracked.FechaRespuesta = DateTime.Now;
                }
            }
        }
        await domicilios.GuardarAsync();
    }

    /// <summary>Valida y copia los datos del request a la entidad (usado también en el registro de usuarios).</summary>
    public async Task AplicarAsync(Domicilio d, DomicilioRequest req)
    {
        _ = await geo.ObtenerCiudadAsync(req.IdCiudad) ?? throw new NegocioException("La ciudad seleccionada no existe");

        d.Calle = req.Calle.Trim();
        d.Numero = req.Numero;
        d.Departamento = Mapeos.Limpiar(req.Departamento);
        d.Piso = Mapeos.Limpiar(req.Piso);
        d.Block = Mapeos.Limpiar(req.Block);
        d.Manzana = Mapeos.Limpiar(req.Manzana);
        d.IdCiudad = req.IdCiudad;
        d.CodigoPostal = req.CodigoPostal.Trim().ToUpperInvariant();
        d.Telefono = Mapeos.Limpiar(req.Telefono);

        var ids = req.IdsFuentes.Distinct().ToList();
        var encontradas = await fuentes.ObtenerPorIdsAsync(ids);
        if (encontradas.Count != ids.Count || encontradas.Any(f => f.FechaBaja != null))
            throw new NegocioException("Alguna de las fuentes de emisión seleccionadas no existe o fue dada de baja");

        var quitar = d.Fuentes.Where(f => !ids.Contains(f.IdFuenteEmision)).ToList();
        domicilios.QuitarFuentes(quitar);
        foreach (var q in quitar) d.Fuentes.Remove(q);
        foreach (var id in ids.Where(id => d.Fuentes.All(f => f.IdFuenteEmision != id)))
            d.Fuentes.Add(new DomicilioFuente { IdFuenteEmision = id });
    }

    private async Task<Domicilio> ObtenerPropioAsync(long idUsuario, long idDomicilio)
    {
        var d = await domicilios.ObtenerCompletoAsync(idDomicilio) ?? throw new NoEncontradoException("Domicilio no encontrado");
        if (d.IdUsuario != idUsuario) throw new ProhibidoException("El domicilio no te pertenece");
        return d;
    }

    private async Task<DomicilioDto> ADtoAsync(Domicilio d)
    {
        var c = (await domicilios.ConteosAsync([d.IdDomicilio]))[d.IdDomicilio];
        return Mapeos.ToDto(d, c.Registros, c.Colaboradores);
    }
}
