using HDC.BLL.Comun;
using HDC.DAL.Repositorios;
using HDC.Entity.Dtos;
using HDC.Entity.Entities;

namespace HDC.BLL.Servicios;

public class CatalogoServicio(
    AlcanceRepositorio alcances,
    UnidadMedidaRepositorio unidades,
    TipoEmisionRepositorio tipos,
    GeoRepositorio geo)
{
    // ---------- Alcances ----------
    public async Task<List<AlcanceDto>> ListarAlcancesAsync() =>
        (await alcances.ListarAsync()).Select(a => new AlcanceDto(a.IdAlcance, a.Nombre, a.Descripcion, a.TiposEmision.Count)).ToList();

    public async Task<AlcanceDto> GuardarAlcanceAsync(long? id, AlcanceRequest req)
    {
        var nombre = req.Nombre.Trim();
        if (await alcances.ExisteNombreAsync(nombre, id)) throw new ConflictoException("Ya existe un alcance con ese nombre");
        var a = id is long existente
            ? await alcances.ObtenerPorIdAsync(existente) ?? throw new NoEncontradoException("Alcance no encontrado")
            : new Alcance();
        a.Nombre = nombre;
        a.Descripcion = Mapeos.Limpiar(req.Descripcion);
        if (id is null) alcances.Agregar(a);
        await alcances.GuardarAsync();
        return new AlcanceDto(a.IdAlcance, a.Nombre, a.Descripcion, 0);
    }

    public async Task EliminarAlcanceAsync(long id)
    {
        var a = await alcances.ObtenerPorIdAsync(id) ?? throw new NoEncontradoException("Alcance no encontrado");
        if (await alcances.EnUsoAsync(id)) throw new ConflictoException("El alcance tiene tipos de emisión asociados");
        alcances.Eliminar(a);
        await alcances.GuardarAsync();
    }

    // ---------- Unidades de medida ----------
    public async Task<List<UnidadMedidaDto>> ListarUnidadesAsync() =>
        (await unidades.ListarConUsoAsync()).Select(x => new UnidadMedidaDto(x.Unidad.IdUnidadMedida, x.Unidad.Nombre, x.Fuentes)).ToList();

    public async Task<UnidadMedidaDto> GuardarUnidadAsync(long? id, UnidadMedidaRequest req)
    {
        var nombre = req.Nombre.Trim();
        if (await unidades.ExisteNombreAsync(nombre, id)) throw new ConflictoException("Ya existe una unidad con ese nombre");
        var u = id is long existente
            ? await unidades.ObtenerPorIdAsync(existente) ?? throw new NoEncontradoException("Unidad no encontrada")
            : new UnidadMedida();
        u.Nombre = nombre;
        if (id is null) unidades.Agregar(u);
        await unidades.GuardarAsync();
        return new UnidadMedidaDto(u.IdUnidadMedida, u.Nombre, 0);
    }

    public async Task EliminarUnidadAsync(long id)
    {
        var u = await unidades.ObtenerPorIdAsync(id) ?? throw new NoEncontradoException("Unidad no encontrada");
        if (await unidades.EnUsoAsync(id)) throw new ConflictoException("La unidad está asignada a fuentes de emisión");
        unidades.Eliminar(u);
        await unidades.GuardarAsync();
    }

    // ---------- Tipos de emisión ----------
    public async Task<List<TipoEmisionDto>> ListarTiposAsync() =>
        (await tipos.ListarConUsoAsync())
            .Select(x => new TipoEmisionDto(x.Tipo.IdTipoEmision, x.Tipo.IdAlcance, x.Tipo.Alcance.Nombre, x.Tipo.Nombre, x.Fuentes))
            .ToList();

    public async Task<TipoEmisionDto> GuardarTipoAsync(long? id, TipoEmisionRequest req)
    {
        var alcance = await alcances.ObtenerPorIdAsync(req.IdAlcance) ?? throw new NegocioException("El alcance no existe");
        var t = id is long existente
            ? await tipos.ObtenerPorIdAsync(existente) ?? throw new NoEncontradoException("Tipo de emisión no encontrado")
            : new TipoEmision();
        t.IdAlcance = req.IdAlcance;
        t.Nombre = req.Nombre.Trim();
        if (id is null) tipos.Agregar(t);
        await tipos.GuardarAsync();
        return new TipoEmisionDto(t.IdTipoEmision, t.IdAlcance, alcance.Nombre, t.Nombre, 0);
    }

    public async Task EliminarTipoAsync(long id)
    {
        var t = await tipos.ObtenerPorIdAsync(id) ?? throw new NoEncontradoException("Tipo de emisión no encontrado");
        if (await tipos.EnUsoAsync(id)) throw new ConflictoException("El tipo de emisión está en uso por fuentes o colaboraciones");
        tipos.Eliminar(t);
        await tipos.GuardarAsync();
    }

    // ---------- Geografía ----------
    public async Task<List<ProvinciaDto>> ListarProvinciasAsync() =>
        (await geo.ListarProvinciasAsync()).Select(p => new ProvinciaDto(p.IdProvincia, p.Nombre)).ToList();

    public async Task<List<DistritoDto>> ListarDistritosAsync(long idProvincia) =>
        (await geo.ListarDistritosAsync(idProvincia)).Select(d => new DistritoDto(d.IdDistrito, d.IdProvincia, d.Nombre)).ToList();

    public async Task<List<CiudadDto>> ListarCiudadesAsync(long idDistrito) =>
        (await geo.ListarCiudadesAsync(idDistrito)).Select(c => new CiudadDto(c.IdCiudad, c.IdDistrito, c.Nombre)).ToList();
}

public class FuenteEmisionServicio(FuenteEmisionRepositorio fuentes, UnidadMedidaRepositorio unidades, TipoEmisionRepositorio tipos)
{
    public async Task<List<FuenteEmisionDto>> ListarAsync(bool incluirBajas) =>
        (await fuentes.ListarAsync(incluirBajas)).Select(Mapeos.ToDto).ToList();

    public async Task<FuenteEmisionDto> CrearAsync(FuenteEmisionRequest req)
    {
        await ValidarAsync(req);
        var f = new FuenteEmision { FechaAlta = DateTime.Now };
        Aplicar(f, req);
        fuentes.Agregar(f);
        await fuentes.GuardarAsync();
        return Mapeos.ToDto((await fuentes.ObtenerCompletaAsync(f.IdFuenteEmision))!);
    }

    public async Task<FuenteEmisionDto> ActualizarAsync(long id, FuenteEmisionRequest req)
    {
        await ValidarAsync(req);
        var f = await fuentes.ObtenerCompletaAsync(id) ?? throw new NoEncontradoException("Fuente de emisión no encontrada");
        Aplicar(f, req);
        f.FechaModificacion = DateTime.Now;
        await fuentes.GuardarAsync();
        return Mapeos.ToDto((await fuentes.ObtenerCompletaAsync(id))!);
    }

    /// <summary>Si la fuente ya se usó en registros se da de baja lógica para conservar el historial.</summary>
    public async Task<bool> EliminarAsync(long id)
    {
        var f = await fuentes.ObtenerPorIdAsync(id) ?? throw new NoEncontradoException("Fuente de emisión no encontrada");
        if (await fuentes.EnUsoAsync(id))
        {
            f.FechaBaja = DateTime.Now;
            await fuentes.GuardarAsync();
            return false;
        }
        fuentes.Eliminar(f);
        await fuentes.GuardarAsync();
        return true;
    }

    public async Task<FuenteEmisionDto> ReactivarAsync(long id)
    {
        var f = await fuentes.ObtenerCompletaAsync(id) ?? throw new NoEncontradoException("Fuente de emisión no encontrada");
        f.FechaBaja = null;
        f.FechaModificacion = DateTime.Now;
        await fuentes.GuardarAsync();
        return Mapeos.ToDto(f);
    }

    private async Task ValidarAsync(FuenteEmisionRequest req)
    {
        _ = await unidades.ObtenerPorIdAsync(req.IdUnidadMedida) ?? throw new NegocioException("La unidad de medida no existe");
        _ = await tipos.ObtenerPorIdAsync(req.IdTipoEmision) ?? throw new NegocioException("El tipo de emisión no existe");
    }

    private static void Aplicar(FuenteEmision f, FuenteEmisionRequest req)
    {
        f.Nombre = req.Nombre.Trim();
        f.ValorFactorEmision = req.ValorFactorEmision;
        f.IdUnidadMedida = req.IdUnidadMedida;
        f.IdTipoEmision = req.IdTipoEmision;
    }
}
