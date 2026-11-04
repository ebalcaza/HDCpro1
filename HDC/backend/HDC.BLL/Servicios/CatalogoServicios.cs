using HDC.BLL.Comun;
using HDC.DAL.Repositorios;
using HDC.Entity.Dtos;
using HDC.Entity.Entities;

namespace HDC.BLL.Servicios;

public class CatalogoServicio(
    AlcanceRepositorio alcances,
    UnidadMedidaRepositorio unidades,
    TipoEmisionRepositorio tipos,
    GeoRepositorio geo,
    CacheCatalogos cache)
{
    // Los listados pasan por la cache: la base se consulta solo la primera vez o después de un cambio.

    // ---------- Alcances ----------
    public Task<List<AlcanceDto>> ListarAlcancesAsync() =>
        cache.ObtenerAsync(CacheCatalogos.GrupoCatalogos, "alcances", async () =>
            (await alcances.ListarAsync()).Select(a => new AlcanceDto(a.IdAlcance, a.Nombre, a.Descripcion, a.TiposEmision.Count)).ToList());

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
        cache.Invalidar(CacheCatalogos.GrupoCatalogos); // los listados cacheados quedaron viejos
        return new AlcanceDto(a.IdAlcance, a.Nombre, a.Descripcion, 0);
    }

    public async Task EliminarAlcanceAsync(long id)
    {
        var a = await alcances.ObtenerPorIdAsync(id) ?? throw new NoEncontradoException("Alcance no encontrado");
        if (await alcances.EnUsoAsync(id)) throw new ConflictoException("El alcance tiene tipos de emisión asociados");
        alcances.Eliminar(a);
        await alcances.GuardarAsync();
        cache.Invalidar(CacheCatalogos.GrupoCatalogos); // los listados cacheados quedaron viejos
    }

    // ---------- Unidades de medida ----------
    public Task<List<UnidadMedidaDto>> ListarUnidadesAsync() =>
        cache.ObtenerAsync(CacheCatalogos.GrupoCatalogos, "unidades", async () =>
            (await unidades.ListarConUsoAsync()).Select(x => new UnidadMedidaDto(x.Unidad.IdUnidadMedida, x.Unidad.Nombre, x.Fuentes)).ToList());

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
        cache.Invalidar(CacheCatalogos.GrupoCatalogos); // los listados cacheados quedaron viejos
        return new UnidadMedidaDto(u.IdUnidadMedida, u.Nombre, 0);
    }

    public async Task EliminarUnidadAsync(long id)
    {
        var u = await unidades.ObtenerPorIdAsync(id) ?? throw new NoEncontradoException("Unidad no encontrada");
        if (await unidades.EnUsoAsync(id)) throw new ConflictoException("La unidad está asignada a fuentes de emisión");
        unidades.Eliminar(u);
        await unidades.GuardarAsync();
        cache.Invalidar(CacheCatalogos.GrupoCatalogos); // los listados cacheados quedaron viejos
    }

    // ---------- Tipos de emisión ----------
    public Task<List<TipoEmisionDto>> ListarTiposAsync() =>
        cache.ObtenerAsync(CacheCatalogos.GrupoCatalogos, "tipos", async () =>
            (await tipos.ListarConUsoAsync())
                .Select(x => new TipoEmisionDto(x.Tipo.IdTipoEmision, x.Tipo.IdAlcance, x.Tipo.Alcance.Nombre, x.Tipo.Nombre, x.Fuentes, x.Tipo.ModoCarga))
                .ToList());

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
        cache.Invalidar(CacheCatalogos.GrupoCatalogos); // los listados cacheados quedaron viejos
        return new TipoEmisionDto(t.IdTipoEmision, t.IdAlcance, alcance.Nombre, t.Nombre, 0, t.ModoCarga);
    }

    public async Task EliminarTipoAsync(long id)
    {
        var t = await tipos.ObtenerPorIdAsync(id) ?? throw new NoEncontradoException("Tipo de emisión no encontrado");
        if (await tipos.EnUsoAsync(id)) throw new ConflictoException("El tipo de emisión está en uso por fuentes o colaboraciones");
        tipos.Eliminar(t);
        await tipos.GuardarAsync();
        cache.Invalidar(CacheCatalogos.GrupoCatalogos); // los listados cacheados quedaron viejos
    }

    // ---------- Geografía ----------
    // Se cachea cada provincia/distrito por separado (clave con el id); se invalida al sincronizar con georef.
    public Task<List<ProvinciaDto>> ListarProvinciasAsync() =>
        cache.ObtenerAsync(CacheCatalogos.GrupoGeo, "provincias", async () =>
            (await geo.ListarProvinciasAsync()).Select(p => new ProvinciaDto(p.IdProvincia, p.Nombre)).ToList());

    public Task<List<DistritoDto>> ListarDistritosAsync(long idProvincia) =>
        cache.ObtenerAsync(CacheCatalogos.GrupoGeo, $"distritos:{idProvincia}", async () =>
            (await geo.ListarDistritosAsync(idProvincia)).Select(d => new DistritoDto(d.IdDistrito, d.IdProvincia, d.Nombre)).ToList());

    public Task<List<CiudadDto>> ListarCiudadesAsync(long idDistrito) =>
        cache.ObtenerAsync(CacheCatalogos.GrupoGeo, $"ciudades:{idDistrito}", async () =>
            (await geo.ListarCiudadesAsync(idDistrito)).Select(c => new CiudadDto(c.IdCiudad, c.IdDistrito, c.Nombre)).ToList());
}

public class FuenteEmisionServicio(FuenteEmisionRepositorio fuentes, UnidadMedidaRepositorio unidades, TipoEmisionRepositorio tipos, CacheCatalogos cache)
{
    // Es el catálogo que más se pide (cada vez que se abre el formulario de un registro): se cachea por variante
    public Task<List<FuenteEmisionDto>> ListarAsync(bool incluirBajas) =>
        cache.ObtenerAsync(CacheCatalogos.GrupoCatalogos, $"fuentes:{incluirBajas}", async () =>
            (await fuentes.ListarAsync(incluirBajas)).Select(Mapeos.ToDto).ToList());

    public async Task<FuenteEmisionDto> CrearAsync(FuenteEmisionRequest req)
    {
        var fuenteBase = await ValidarAsync(req, null);
        var f = new FuenteEmision { FechaAlta = DateTime.Now };
        Aplicar(f, req, fuenteBase);
        fuentes.Agregar(f);
        await fuentes.GuardarAsync();
        cache.Invalidar(CacheCatalogos.GrupoCatalogos); // los listados cacheados quedaron viejos
        return Mapeos.ToDto((await fuentes.ObtenerCompletaAsync(f.IdFuenteEmision))!);
    }

    public async Task<FuenteEmisionDto> ActualizarAsync(long id, FuenteEmisionRequest req)
    {
        var fuenteBase = await ValidarAsync(req, id);
        var f = await fuentes.ObtenerCompletaAsync(id) ?? throw new NoEncontradoException("Fuente de emisión no encontrada");
        Aplicar(f, req, fuenteBase);
        f.FechaModificacion = DateTime.Now;
        // Las fuentes calculadas desde esta siguen su factor. Lo ya cargado conserva el factor con el que se calculó.
        foreach (var derivada in await fuentes.DerivadasDeAsync(id))
        {
            derivada.ValorFactorEmision = FactorDerivado(f.ValorFactorEmision, derivada.ConsumoPorKm!.Value);
            derivada.FechaModificacion = f.FechaModificacion;
        }
        await fuentes.GuardarAsync();
        cache.Invalidar(CacheCatalogos.GrupoCatalogos); // los listados cacheados quedaron viejos
        return Mapeos.ToDto((await fuentes.ObtenerCompletaAsync(id))!);
    }

    /// <summary>Si la fuente ya se usó en registros se da de baja lógica para conservar el historial.</summary>
    public async Task<bool> EliminarAsync(long id)
    {
        var f = await fuentes.ObtenerPorIdAsync(id) ?? throw new NoEncontradoException("Fuente de emisión no encontrada");
        // Si otra fuente calcula su factor con esta, tampoco se puede borrar: se da de baja
        if (await fuentes.EnUsoAsync(id) || await fuentes.TieneDerivadasAsync(id))
        {
            f.FechaBaja = DateTime.Now;
            await fuentes.GuardarAsync();
        cache.Invalidar(CacheCatalogos.GrupoCatalogos); // los listados cacheados quedaron viejos
            return false;
        }
        fuentes.Eliminar(f);
        await fuentes.GuardarAsync();
        cache.Invalidar(CacheCatalogos.GrupoCatalogos); // los listados cacheados quedaron viejos
        return true;
    }

    public async Task<FuenteEmisionDto> ReactivarAsync(long id)
    {
        var f = await fuentes.ObtenerCompletaAsync(id) ?? throw new NoEncontradoException("Fuente de emisión no encontrada");
        f.FechaBaja = null;
        f.FechaModificacion = DateTime.Now;
        await fuentes.GuardarAsync();
        cache.Invalidar(CacheCatalogos.GrupoCatalogos); // los listados cacheados quedaron viejos
        return Mapeos.ToDto(f);
    }

    /// <summary>Valida el pedido y devuelve la fuente base, si el factor se calcula desde otra fuente.</summary>
    private async Task<FuenteEmision?> ValidarAsync(FuenteEmisionRequest req, long? idPropia)
    {
        _ = await unidades.ObtenerPorIdAsync(req.IdUnidadMedida) ?? throw new NegocioException("La unidad de medida no existe");
        var tipo = await tipos.ObtenerPorIdAsync(req.IdTipoEmision) ?? throw new NegocioException("El tipo de emisión no existe");

        if (req.IdFuenteBase is not long idBase)
        {
            if (req.ConsumoPorKm != null) throw new NegocioException("Elegí de qué fuente sale el factor");
            return null;
        }
        if (req.ConsumoPorKm is null) throw new NegocioException("Indicá el consumo por km");
        if (tipo.ModoCarga != ModoCarga.Viaje) throw new NegocioException("Solo los viajes institucionales calculan su factor desde otra fuente");
        if (idBase == idPropia) throw new NegocioException("Una fuente no puede calcular su factor desde sí misma");

        var fuenteBase = await fuentes.ObtenerCompletaAsync(idBase) ?? throw new NegocioException("La fuente de la que sale el factor no existe");
        if (fuenteBase.FechaBaja != null) throw new NegocioException($"La fuente \"{fuenteBase.Nombre}\" fue dada de baja");
        // Un solo nivel: la base tiene que ser un consumo con factor propio, para que el cálculo se pueda seguir
        if (fuenteBase.IdFuenteBase != null || fuenteBase.TipoEmision.ModoCarga is ModoCarga.Viaje or ModoCarga.Traslado)
            throw new NegocioException($"\"{fuenteBase.Nombre}\" no se puede usar como base: elegí un combustible o la electricidad");
        if (idPropia is long propia && await fuentes.TieneDerivadasAsync(propia))
            throw new NegocioException("Otras fuentes calculan su factor con esta: no puede depender de otra");
        return fuenteBase;
    }

    private static decimal FactorDerivado(decimal factorBase, decimal consumoPorKm) => Math.Round(factorBase * consumoPorKm, 6);

    private static void Aplicar(FuenteEmision f, FuenteEmisionRequest req, FuenteEmision? fuenteBase)
    {
        f.Nombre = req.Nombre.Trim();
        f.Descripcion = Mapeos.Limpiar(req.Descripcion);
        // Con una fuente base, el factor siempre se calcula: se ignora el valor que venga en el pedido
        f.ValorFactorEmision = fuenteBase != null ? FactorDerivado(fuenteBase.ValorFactorEmision, req.ConsumoPorKm!.Value) : req.ValorFactorEmision;
        f.IdFuenteBase = fuenteBase?.IdFuenteEmision;
        f.ConsumoPorKm = fuenteBase != null ? req.ConsumoPorKm : null;
        f.IdUnidadMedida = req.IdUnidadMedida;
        f.IdTipoEmision = req.IdTipoEmision;
    }
}
