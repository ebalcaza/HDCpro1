using System.Text.Json;
using HDC.BLL.Comun;
using HDC.DAL.Repositorios;
using HDC.Entity.Dtos;
using HDC.Entity.Entities;

namespace HDC.BLL.Servicios;

/// <summary>
/// Dashboard y reportes.
/// - Dashboard: las sumas (GROUP BY) las hace SQL Server y acá solo se arman los porcentajes y la serie mensual.
/// - Reportes: necesitan el detalle fila por fila (tabla y PDF), así que se traen las emisiones "planas"
///   filtradas y, como ya están en memoria, se agrupan acá sin volver a consultar la base.
/// </summary>
public class EstadisticaServicio(
    RegistroRepositorio registros,
    ReporteRepositorio reportes,
    DomicilioRepositorio domicilios,
    AlcanceRepositorio alcances,
    TipoEmisionRepositorio tipos,
    FuenteEmisionRepositorio fuentes,
    RolRepositorio roles,
    GeoRepositorio geo)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ======================= DASHBOARD =======================

    public async Task<DashboardDto> DashboardAsync(long idUsuario, long? idDomicilio, DateOnly? desde, DateOnly? hasta)
    {
        var filtro = new FiltroEmisiones
        {
            IdsUsuarios = [idUsuario],
            Desde = desde is DateOnly d ? Mapeos.InicioDeMes(d) : null,
            Hasta = hasta is DateOnly h ? Mapeos.InicioDeMes(h) : null,
        };
        if (idDomicilio is long id) filtro.IdsDomicilios.Add(id);

        // Todo llega ya sumado por SQL Server: acá solo se combinan unas pocas filas (meses × alcances, tipos, fuentes...)
        var a = await registros.ObtenerAgregadosAsync(filtro);

        var total = a.PorPeriodoAlcance.Sum(x => x.KgCo2);
        var mes = Mapeos.MesActual();
        var mesActual = a.PorPeriodoAlcance.Where(x => x.Periodo == mes).Sum(x => x.KgCo2);
        var mesAnterior = a.PorPeriodoAlcance.Where(x => x.Periodo == mes.AddMonths(-1)).Sum(x => x.KgCo2);
        decimal? variacion = mesAnterior > 0 ? Math.Round((mesActual - mesAnterior) / mesAnterior * 100, 2) : null;

        // Dona por alcance: se juntan los meses de cada alcance
        var porAlcance = a.PorPeriodoAlcance
            .GroupBy(x => new { x.IdAlcance, x.Alcance })
            .Select(g => new SerieItemDto(g.Key.IdAlcance, g.Key.Alcance, g.Sum(x => x.KgCo2), Porcentaje(g.Sum(x => x.KgCo2), total)))
            .OrderByDescending(s => s.KgCo2)
            .ToList();

        var evolucion = Evolucion(a.PorPeriodoAlcance.Select(x => (x.Periodo, x.Alcance, x.KgCo2)));
        var conDatos = evolucion.Where(e => e.KgCo2 > 0).ToList();

        var lectura = new LecturaRapidaDto(
            evolucion.Count > 0 ? Math.Round(total / evolucion.Count, 4) : 0,
            conDatos.MaxBy(e => e.KgCo2) is { } max ? new PeriodoValorDto(max.Periodo, max.KgCo2) : null,
            conDatos.MinBy(e => e.KgCo2) is { } min ? new PeriodoValorDto(min.Periodo, min.KgCo2) : null,
            a.CantidadRegistros,
            conDatos.Count,
            a.CantidadDomicilios,
            a.PorColaborador.Count); // cada grupo es un colaborador distinto

        return new DashboardDto(
            total, mesActual, mesAnterior, variacion,
            porAlcance.FirstOrDefault(),
            porAlcance,
            ASerie(a.PorTipo, total),
            ASerie(a.PorFuente, total),
            ASerie(a.PorDomicilio, total),
            ASerie(a.PorColaborador, total),
            evolucion,
            lectura);
    }

    /// <summary>Convierte los grupos sumados por SQL en la serie que espera el frontend (con % y ordenada de mayor a menor).</summary>
    private static List<SerieItemDto> ASerie(IEnumerable<SumaGrupo> grupos, decimal total) =>
        grupos
            .Select(g => new SerieItemDto(g.Id, g.Nombre, g.KgCo2, Porcentaje(g.KgCo2, total), g.Grupo,
                g.Unidad != null ? g.Consumo : null, g.Unidad)) // consumo y unidad solo tienen sentido en fuentes
            .OrderByDescending(s => s.KgCo2)
            .ToList();

    // ======================= REPORTES =======================

    public async Task<ReporteResultadoDto> GenerarReporteAsync(long idUsuario, FiltroEmisiones filtros, bool global)
    {
        var f = Normalizar(filtros, idUsuario, global);
        var datos = await registros.ObtenerEmisionesAsync(f);
        var total = datos.Sum(x => x.KgCo2);
        var evolucion = Evolucion(datos.Select(x => (x.Periodo, x.Alcance, x.KgCo2)));

        var resumen = new ReporteResumenDto(
            total,
            datos.Select(x => x.IdRegistro).Distinct().Count(),
            datos.Count,
            datos.Select(x => x.IdDomicilio).Distinct().Count(),
            datos.Select(x => x.IdUsuarioTitular).Distinct().Count(),
            evolucion.FirstOrDefault()?.Periodo,
            evolucion.LastOrDefault()?.Periodo,
            evolucion.Count > 0 ? Math.Round(total / evolucion.Count, 4) : 0);

        var filas = datos
            .OrderBy(x => x.Periodo).ThenBy(x => x.Titular).ThenBy(x => x.Alcance).ThenByDescending(x => x.KgCo2)
            .Select(x => new ReporteFilaDto(
                x.IdRegistro, Mapeos.FormatoPeriodo(x.Periodo), x.DomicilioTexto, x.Titular, x.Rol,
                x.Provincia, x.Distrito, x.Ciudad, x.Alcance, x.TipoEmision, x.Fuente,
                x.Consumo, x.UnidadMedida, x.ValorFactorEmision, x.KgCo2))
            .ToList();

        return new ReporteResultadoDto(
            DateTime.Now, global,
            await DescribirFiltrosAsync(f, global),
            resumen,
            evolucion.Select(e => new PeriodoValorDto(e.Periodo, e.KgCo2)).ToList(),
            Agrupar(datos, x => x.IdAlcance, x => x.Alcance, total),
            Agrupar(datos, x => x.IdTipoEmision, x => x.TipoEmision, total, x => x.Alcance),
            PorFuente(datos, total),
            Agrupar(datos, x => x.IdDomicilio, x => $"{x.DomicilioTexto} - {x.Ciudad}", total, x => x.Titular),
            global ? Agrupar(datos, x => x.IdRol, x => x.Rol, total) : [],
            Agrupar(datos, x => x.IdProvincia, x => x.Provincia, total),
            Agrupar(datos, x => x.IdDistrito, x => x.Distrito, total, x => x.Provincia),
            global ? Agrupar(datos, x => x.IdUsuarioTitular, x => x.Titular, total, x => x.Rol) : [],
            filas);
    }

    public async Task<List<ReporteGuardadoDto>> ListarGuardadosAsync(long idUsuario, bool global) =>
        (await reportes.ListarPorUsuarioAsync(idUsuario))
            .Select(ADto)
            .Where(r => r.EsGlobal == global)
            .ToList();

    public async Task<ReporteGuardadoDto> GuardarAsync(long idUsuario, GuardarReporteRequest req)
    {
        var r = new Reporte
        {
            IdUsuario = idUsuario,
            Nombre = req.Nombre.Trim(),
            Descripcion = Mapeos.Limpiar(req.Descripcion),
            Filtros = JsonSerializer.Serialize(new FiltrosGuardados(req.EsGlobal, req.Filtros), Json),
            FechaCreacion = DateTime.Now
        };
        reportes.Agregar(r);
        await reportes.GuardarAsync();
        return ADto(r);
    }

    public async Task EliminarGuardadoAsync(long idUsuario, long idReporte)
    {
        var r = await reportes.ObtenerPorIdAsync(idReporte) ?? throw new NoEncontradoException("Reporte no encontrado");
        if (r.IdUsuario != idUsuario) throw new ProhibidoException();
        reportes.Eliminar(r);
        await reportes.GuardarAsync();
    }

    private record FiltrosGuardados(bool EsGlobal, FiltroEmisiones Filtros);

    private static ReporteGuardadoDto ADto(Reporte r)
    {
        var g = JsonSerializer.Deserialize<FiltrosGuardados>(r.Filtros, Json) ?? new FiltrosGuardados(false, new());
        return new ReporteGuardadoDto(r.IdReporte, r.Nombre, r.Descripcion, g.EsGlobal, g.Filtros, r.FechaCreacion);
    }

    /// <summary>En reportes de usuario se limita a sus domicilios y se ignoran los filtros exclusivos del admin.</summary>
    private static FiltroEmisiones Normalizar(FiltroEmisiones f, long idUsuario, bool global) => new()
    {
        Desde = f.Desde is DateOnly d ? Mapeos.InicioDeMes(d) : null,
        Hasta = f.Hasta is DateOnly h ? Mapeos.InicioDeMes(h) : null,
        IdsDomicilios = f.IdsDomicilios.Distinct().ToList(),
        IdsAlcances = f.IdsAlcances.Distinct().ToList(),
        IdsTiposEmision = f.IdsTiposEmision.Distinct().ToList(),
        IdsFuentes = f.IdsFuentes.Distinct().ToList(),
        IdProvincia = f.IdProvincia,
        IdDistrito = f.IdDistrito,
        IdCiudad = f.IdCiudad,
        IdsRoles = global ? f.IdsRoles.Distinct().ToList() : [],
        IdsUsuarios = global ? f.IdsUsuarios.Distinct().ToList() : [idUsuario],
    };

    private async Task<List<string>> DescribirFiltrosAsync(FiltroEmisiones f, bool global)
    {
        var res = new List<string>();
        if (f.Desde != null || f.Hasta != null)
            res.Add($"Período: {(f.Desde is DateOnly d ? Mapeos.FormatoPeriodo(d) : "inicio")} a {(f.Hasta is DateOnly h ? Mapeos.FormatoPeriodo(h) : "actualidad")}");
        if (f.IdsAlcances.Count > 0)
            res.Add("Alcances: " + string.Join(", ", (await alcances.ListarAsync()).Where(a => f.IdsAlcances.Contains(a.IdAlcance)).Select(a => a.Nombre)));
        if (f.IdsTiposEmision.Count > 0)
            res.Add("Tipos de emisión: " + string.Join(", ", (await tipos.ObtenerPorIdsAsync(f.IdsTiposEmision)).Select(t => t.Nombre)));
        if (f.IdsFuentes.Count > 0)
            res.Add("Fuentes: " + string.Join(", ", (await fuentes.ObtenerPorIdsAsync(f.IdsFuentes)).Select(x => x.Nombre)));
        if (f.IdsDomicilios.Count > 0)
        {
            var nombres = new List<string>();
            foreach (var id in f.IdsDomicilios)
                if (await domicilios.ObtenerCompletoAsync(id) is { } dom) nombres.Add(Mapeos.DescripcionCorta(dom));
            res.Add("Domicilios: " + string.Join("; ", nombres));
        }
        if (global && f.IdsRoles.Count > 0)
            res.Add("Tipos de usuario: " + string.Join(", ", (await roles.ListarAsync()).Where(r => f.IdsRoles.Contains(r.IdRol)).Select(r => r.Nombre)));
        if (f.IdCiudad is long idC && await geo.ObtenerCiudadAsync(idC) is { } c)
            res.Add($"Zona: {c.Nombre}, {c.Distrito.Nombre}, {c.Distrito.Provincia.Nombre}");
        else if (f.IdDistrito is long idD && f.IdProvincia is long idP1)
        {
            var dist = (await geo.ListarDistritosAsync(idP1)).FirstOrDefault(x => x.IdDistrito == idD);
            var prov = (await geo.ListarProvinciasAsync()).FirstOrDefault(p => p.IdProvincia == idP1);
            res.Add($"Zona: {dist?.Nombre}, {prov?.Nombre}");
        }
        else if (f.IdProvincia is long idP)
            res.Add($"Zona: {(await geo.ListarProvinciasAsync()).FirstOrDefault(p => p.IdProvincia == idP)?.Nombre}");
        if (res.Count == 0) res.Add("Sin filtros: todas las emisiones registradas");
        return res;
    }

    // ======================= AGREGACIONES =======================

    private static List<SerieItemDto> Agrupar(
        IEnumerable<EmisionPlana> datos, Func<EmisionPlana, long> clave, Func<EmisionPlana, string> nombre,
        decimal total, Func<EmisionPlana, string>? grupo = null) =>
        datos.GroupBy(clave)
            .Select(g => new SerieItemDto(
                g.Key, nombre(g.First()), g.Sum(x => x.KgCo2),
                Porcentaje(g.Sum(x => x.KgCo2), total),
                grupo?.Invoke(g.First())))
            .OrderByDescending(s => s.KgCo2)
            .ToList();

    private static List<SerieItemDto> PorFuente(IEnumerable<EmisionPlana> datos, decimal total) =>
        datos.GroupBy(x => x.IdFuenteEmision)
            .Select(g => new SerieItemDto(
                g.Key, g.First().Fuente, g.Sum(x => x.KgCo2), Porcentaje(g.Sum(x => x.KgCo2), total),
                g.First().TipoEmision, g.Sum(x => x.Consumo), g.First().UnidadMedida))
            .OrderByDescending(s => s.KgCo2)
            .ToList();

    /// <summary>
    /// Serie mensual continua (los meses sin datos quedan en 0) con el desglose por alcance.
    /// Recibe tuplas (mes, alcance, kg): el Dashboard le pasa las sumas que ya calculó SQL
    /// y el reporte le pasa sus filas de detalle; el resultado es el mismo.
    /// </summary>
    private static List<PuntoEvolucionDto> Evolucion(IEnumerable<(DateOnly Periodo, string Alcance, decimal KgCo2)> datos)
    {
        var lista = datos.ToList();
        if (lista.Count == 0) return [];
        var nombresAlcance = lista.Select(x => x.Alcance).Distinct().OrderBy(a => a).ToList();
        var porMes = lista.GroupBy(x => x.Periodo).ToDictionary(g => g.Key, g => g.ToList());
        var res = new List<PuntoEvolucionDto>();
        for (var p = porMes.Keys.Min(); p <= porMes.Keys.Max(); p = p.AddMonths(1))
        {
            var items = porMes.GetValueOrDefault(p) ?? [];
            res.Add(new PuntoEvolucionDto(
                Mapeos.FormatoPeriodo(p),
                items.Sum(x => x.KgCo2),
                nombresAlcance.ToDictionary(a => a, a => items.Where(x => x.Alcance == a).Sum(x => x.KgCo2))));
        }
        return res;
    }

    private static decimal Porcentaje(decimal parte, decimal total) => total == 0 ? 0 : Math.Round(parte / total * 100, 2);
}
