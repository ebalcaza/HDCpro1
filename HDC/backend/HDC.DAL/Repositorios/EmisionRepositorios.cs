using HDC.Entity.Dtos;
using HDC.Entity.Entities;
using Microsoft.EntityFrameworkCore;

namespace HDC.DAL.Repositorios;

public class RegistroRepositorio(HdcDbContext ctx) : Repositorio<Registro>(ctx)
{
    private IQueryable<Registro> Completos() => Ctx.Registros
        .Include(r => r.Domicilio).ThenInclude(d => d.Ciudad).ThenInclude(c => c.Distrito).ThenInclude(di => di.Provincia)
        .Include(r => r.Domicilio).ThenInclude(d => d.Usuario).ThenInclude(u => u.Individuo)
        .Include(r => r.Domicilio).ThenInclude(d => d.Usuario).ThenInclude(u => u.Organizacion)
        .Include(r => r.UsuarioCarga).ThenInclude(u => u.Individuo)
        .Include(r => r.UsuarioCarga).ThenInclude(u => u.Organizacion)
        .Include(r => r.Detalles).ThenInclude(d => d.FuenteEmision).ThenInclude(f => f.UnidadMedida)
        .Include(r => r.Detalles).ThenInclude(d => d.FuenteEmision).ThenInclude(f => f.TipoEmision).ThenInclude(t => t.Alcance)
        .AsSplitQuery();

    /// <summary>Registros de los domicilios del usuario más los que él cargó como colaborador (sin Includes: es la base para filtrar).</summary>
    private IQueryable<Registro> Visibles(long idUsuario) => Ctx.Registros.AsNoTracking()
        .Where(r => r.Domicilio.IdUsuario == idUsuario || r.IdUsuarioCarga == idUsuario);

    /// <summary>
    /// Una página de "Mis emisiones" con filtros, búsqueda y orden resueltos en SQL.
    /// Se hace en dos pasos para no traer de más:
    ///   1) con la consulta liviana (sin Includes) se cuenta, se suma y se obtienen solo los ids de la página;
    ///   2) se cargan completos (con domicilio, usuario y detalles) únicamente esos registros.
    /// </summary>
    public async Task<(List<Registro> Pagina, int Total, decimal TotalKgCo2)> PaginarVisiblesAsync(long idUsuario, FiltroRegistros f)
    {
        var q = Visibles(idUsuario);

        // Filtros de la barra superior
        if (f.IdDomicilio is long idDom) q = q.Where(r => r.IdDomicilio == idDom);
        if (f.Desde is DateOnly desde) q = q.Where(r => r.Periodo >= desde);
        if (f.Hasta is DateOnly hasta) q = q.Where(r => r.Periodo <= hasta);

        // Buscador: domicilio, ciudad, quién cargó, alcance o fuente de alguno de sus consumos.
        // LIKE en SQL Server no distingue mayúsculas con la intercalación por defecto.
        if (!string.IsNullOrWhiteSpace(f.Busqueda))
        {
            var t = f.Busqueda.Trim();
            q = q.Where(r =>
                r.Domicilio.Calle.Contains(t) ||
                r.Domicilio.Ciudad.Nombre.Contains(t) ||
                r.UsuarioCarga.Email.Contains(t) ||
                (r.UsuarioCarga.Individuo != null && (r.UsuarioCarga.Individuo.Nombres.Contains(t) || r.UsuarioCarga.Individuo.Apellidos.Contains(t))) ||
                (r.UsuarioCarga.Organizacion != null && r.UsuarioCarga.Organizacion.RazonSocial.Contains(t)) ||
                r.Detalles.Any(d => d.FuenteEmision.Nombre.Contains(t) || d.FuenteEmision.TipoEmision.Alcance.Nombre.Contains(t)));
        }

        // Totales sobre TODO lo filtrado (no solo la página): COUNT y SUM en la base
        var total = await q.CountAsync();
        var totalKg = await q.SumAsync(r => (decimal?)r.TotalKgCo2) ?? 0m;

        // Orden según la columna clickeada en la tabla; el IdRegistro final hace el orden estable entre páginas
        var ordenada = (f.Orden?.ToLowerInvariant()) switch
        {
            "domicilio" => q.OrdenarPor(r => r.Domicilio.Calle, f.Asc).ThenBy(r => r.Domicilio.Numero),
            "usuariocarga" => q.OrdenarPor(r => r.UsuarioCarga.Individuo != null ? r.UsuarioCarga.Individuo.Nombres
                : r.UsuarioCarga.Organizacion != null ? r.UsuarioCarga.Organizacion.RazonSocial : r.UsuarioCarga.Email, f.Asc),
            "cantidaddetalles" => q.OrdenarPor(r => r.Detalles.Count, f.Asc),
            "totalkgco2" => q.OrdenarPor(r => r.TotalKgCo2, f.Asc),
            "periodo" => q.OrdenarPor(r => r.Periodo, f.Asc),
            // Sin orden elegido: los más recientes primero
            _ => q.OrderByDescending(r => r.Periodo).ThenByDescending(r => r.FechaCreacion),
        };
        var ids = await ordenada.ThenBy(r => r.IdRegistro).Paginar(f.Saltear, f.Tamanio).Select(r => r.IdRegistro).ToListAsync();

        // Paso 2: datos completos solo de la página (a lo sumo 100 registros)
        var completos = await Completos().AsNoTracking().Where(r => ids.Contains(r.IdRegistro)).ToListAsync();
        var porId = completos.ToDictionary(r => r.IdRegistro);
        return (ids.Select(id => porId[id]).ToList(), total, totalKg);
    }

    /// <summary>Domicilios (con ciudad) que tienen registros visibles para el usuario: alimentan el filtro de "Mis emisiones".</summary>
    public Task<List<Domicilio>> DomiciliosVisiblesAsync(long idUsuario)
    {
        var ids = Visibles(idUsuario).Select(r => r.IdDomicilio).Distinct();
        return Ctx.Domicilios.AsNoTracking().Include(d => d.Ciudad)
            .Where(d => ids.Contains(d.IdDomicilio))
            .OrderBy(d => d.Calle).ThenBy(d => d.Numero)
            .ToListAsync();
    }

    public Task<Registro?> ObtenerCompletoAsync(long idRegistro) =>
        Completos().FirstOrDefaultAsync(r => r.IdRegistro == idRegistro);

    public Task<List<Registro>> ListarPorDomicilioAsync(long idDomicilio) =>
        Ctx.Registros.Where(r => r.IdDomicilio == idDomicilio).ToListAsync();

    public async Task<decimal> SumarPorDomicilioAsync(long idDomicilio) =>
        await Ctx.Registros.Where(r => r.IdDomicilio == idDomicilio).SumAsync(r => (decimal?)r.TotalKgCo2) ?? 0m;

    public void QuitarDetalles(IEnumerable<DetalleRegistro> detalles) => Ctx.DetallesRegistro.RemoveRange(detalles);

    /// <summary>
    /// Totales del Dashboard calculados por SQL Server (GROUP BY + SUM).
    /// Antes se traía cada consumo como una fila y se sumaba en C#; ahora cada consulta devuelve
    /// pocas filas ya sumadas (una por mes y alcance, una por tipo, etc.), sin importar cuántos consumos haya.
    /// Las consultas se ejecutan una tras otra porque un DbContext no admite consultas en paralelo.
    /// </summary>
    public async Task<AgregadosEmisiones> ObtenerAgregadosAsync(FiltroEmisiones f)
    {
        var q = Filtrar(f);

        // 1) kg por mes y alcance: de acá salen el total, el mes actual/anterior, la dona y la evolución
        var porPeriodoAlcance = await q
            .GroupBy(d => new { d.Registro.Periodo, d.FuenteEmision.TipoEmision.IdAlcance, Alcance = d.FuenteEmision.TipoEmision.Alcance.Nombre })
            .Select(g => new SumaPeriodoAlcance(g.Key.Periodo, g.Key.IdAlcance, g.Key.Alcance, g.Sum(d => d.KgCo2)))
            .ToListAsync();

        // 2) kg por tipo de emisión (el grupo es el alcance al que pertenece)
        var porTipo = await q
            .GroupBy(d => new { d.FuenteEmision.IdTipoEmision, d.FuenteEmision.TipoEmision.Nombre, Alcance = d.FuenteEmision.TipoEmision.Alcance.Nombre })
            .Select(g => new SumaGrupo(g.Key.IdTipoEmision, g.Key.Nombre, g.Key.Alcance, g.Sum(d => d.KgCo2), 0m, null))
            .ToListAsync();

        // 3) kg y consumo por fuente (para "Fuentes que más emiten")
        var porFuente = await q
            .GroupBy(d => new { d.IdFuenteEmision, d.FuenteEmision.Nombre, Tipo = d.FuenteEmision.TipoEmision.Nombre, Unidad = d.FuenteEmision.UnidadMedida.Nombre })
            .Select(g => new SumaGrupo(g.Key.IdFuenteEmision, g.Key.Nombre, g.Key.Tipo, g.Sum(d => d.KgCo2), g.Sum(d => d.Consumo), g.Key.Unidad))
            .ToListAsync();

        // 4) kg por domicilio (el texto "Calle Número - Ciudad" se arma en el servicio)
        var porDomicilio = await q
            .GroupBy(d => new { d.Registro.IdDomicilio, d.Registro.Domicilio.Calle, d.Registro.Domicilio.Numero, Ciudad = d.Registro.Domicilio.Ciudad.Nombre })
            .Select(g => new SumaGrupo(g.Key.IdDomicilio, g.Key.Calle + " " + g.Key.Numero.ToString() + " - " + g.Key.Ciudad, null, g.Sum(d => d.KgCo2), 0m, null))
            .ToListAsync();

        // 5) kg cargados por colaboradores (quien cargó no es el titular del domicilio)
        var porColaborador = await q
            .Where(d => d.Registro.IdUsuarioCarga != d.Registro.Domicilio.IdUsuario)
            .GroupBy(d => new
            {
                d.Registro.IdUsuarioCarga,
                Nombre = d.Registro.UsuarioCarga.Individuo != null
                    ? d.Registro.UsuarioCarga.Individuo.Nombres + " " + d.Registro.UsuarioCarga.Individuo.Apellidos
                    : d.Registro.UsuarioCarga.Organizacion != null
                        ? d.Registro.UsuarioCarga.Organizacion.RazonSocial
                        : d.Registro.UsuarioCarga.Email,
            })
            .Select(g => new SumaGrupo(g.Key.IdUsuarioCarga, g.Key.Nombre, null, g.Sum(d => d.KgCo2), 0m, null))
            .ToListAsync();

        // 6) Conteos con COUNT(DISTINCT ...) en la base
        var cantidadRegistros = await q.Select(d => d.IdRegistro).Distinct().CountAsync();
        var cantidadDomicilios = await q.Select(d => d.Registro.IdDomicilio).Distinct().CountAsync();

        return new AgregadosEmisiones(porPeriodoAlcance, porTipo, porFuente, porDomicilio, porColaborador, cantidadRegistros, cantidadDomicilios);
    }

    /// <summary>Devuelve cada detalle de emisión con todo su contexto, aplicando los filtros en la base (lo usan los reportes, que necesitan el detalle fila por fila).</summary>
    public Task<List<EmisionPlana>> ObtenerEmisionesAsync(FiltroEmisiones f) =>
        Filtrar(f).Select(d => new EmisionPlana
        {
            IdRegistro = d.IdRegistro,
            Periodo = d.Registro.Periodo,
            IdDomicilio = d.Registro.IdDomicilio,
            Calle = d.Registro.Domicilio.Calle,
            Numero = d.Registro.Domicilio.Numero,
            IdUsuarioTitular = d.Registro.Domicilio.IdUsuario,
            Titular = d.Registro.Domicilio.Usuario.Individuo != null
                ? d.Registro.Domicilio.Usuario.Individuo.Nombres + " " + d.Registro.Domicilio.Usuario.Individuo.Apellidos
                : d.Registro.Domicilio.Usuario.Organizacion != null
                    ? d.Registro.Domicilio.Usuario.Organizacion.RazonSocial
                    : d.Registro.Domicilio.Usuario.Email,
            IdRol = d.Registro.Domicilio.Usuario.IdRol,
            Rol = d.Registro.Domicilio.Usuario.Rol.Nombre,
            IdUsuarioCarga = d.Registro.IdUsuarioCarga,
            UsuarioCarga = d.Registro.UsuarioCarga.Individuo != null
                ? d.Registro.UsuarioCarga.Individuo.Nombres + " " + d.Registro.UsuarioCarga.Individuo.Apellidos
                : d.Registro.UsuarioCarga.Organizacion != null
                    ? d.Registro.UsuarioCarga.Organizacion.RazonSocial
                    : d.Registro.UsuarioCarga.Email,
            IdProvincia = d.Registro.Domicilio.Ciudad.Distrito.IdProvincia,
            Provincia = d.Registro.Domicilio.Ciudad.Distrito.Provincia.Nombre,
            IdDistrito = d.Registro.Domicilio.Ciudad.IdDistrito,
            Distrito = d.Registro.Domicilio.Ciudad.Distrito.Nombre,
            IdCiudad = d.Registro.Domicilio.IdCiudad,
            Ciudad = d.Registro.Domicilio.Ciudad.Nombre,
            IdAlcance = d.FuenteEmision.TipoEmision.IdAlcance,
            Alcance = d.FuenteEmision.TipoEmision.Alcance.Nombre,
            IdTipoEmision = d.FuenteEmision.IdTipoEmision,
            TipoEmision = d.FuenteEmision.TipoEmision.Nombre,
            IdFuenteEmision = d.IdFuenteEmision,
            Fuente = d.FuenteEmision.Nombre,
            UnidadMedida = d.FuenteEmision.UnidadMedida.Nombre,
            Consumo = d.Consumo,
            ValorFactorEmision = d.ValorFactorEmision,
            KgCo2 = d.KgCo2
        }).ToListAsync();

    /// <summary>Consulta base de detalles con los filtros de dashboard/reportes aplicados (se traducen al WHERE de SQL).</summary>
    private IQueryable<DetalleRegistro> Filtrar(FiltroEmisiones f)
    {
        var q = Ctx.DetallesRegistro.AsNoTracking().AsQueryable();

        if (f.Desde is DateOnly desde)
            q = q.Where(d => d.Registro.Periodo >= desde);
        if (f.Hasta is DateOnly hasta)
            q = q.Where(d => d.Registro.Periodo <= hasta);
        if (f.IdsDomicilios.Count > 0)
            q = q.Where(d => f.IdsDomicilios.Contains(d.Registro.IdDomicilio));
        if (f.IdsAlcances.Count > 0)
            q = q.Where(d => f.IdsAlcances.Contains(d.FuenteEmision.TipoEmision.IdAlcance));
        if (f.IdsTiposEmision.Count > 0)
            q = q.Where(d => f.IdsTiposEmision.Contains(d.FuenteEmision.IdTipoEmision));
        if (f.IdsFuentes.Count > 0)
            q = q.Where(d => f.IdsFuentes.Contains(d.IdFuenteEmision));
        if (f.IdsRoles.Count > 0)
            q = q.Where(d => f.IdsRoles.Contains(d.Registro.Domicilio.Usuario.IdRol));
        if (f.IdsUsuarios.Count > 0)
            q = q.Where(d => f.IdsUsuarios.Contains(d.Registro.Domicilio.IdUsuario));
        if (f.IdCiudad is long idC)
            q = q.Where(d => d.Registro.Domicilio.IdCiudad == idC);
        else if (f.IdDistrito is long idD)
            q = q.Where(d => d.Registro.Domicilio.Ciudad.IdDistrito == idD);
        else if (f.IdProvincia is long idP)
            q = q.Where(d => d.Registro.Domicilio.Ciudad.Distrito.IdProvincia == idP);

        return q;
    }
}

public class ColaboracionRepositorio(HdcDbContext ctx) : Repositorio<Colaboracion>(ctx)
{
    private IQueryable<Colaboracion> Completas() => Ctx.Colaboraciones
        .Include(c => c.Domicilio).ThenInclude(d => d.Usuario).ThenInclude(u => u.Individuo)
        .Include(c => c.Domicilio).ThenInclude(d => d.Usuario).ThenInclude(u => u.Organizacion)
        .Include(c => c.Colaborador).ThenInclude(u => u.Individuo)
        .Include(c => c.Colaborador).ThenInclude(u => u.Organizacion)
        .Include(c => c.TiposEmision).ThenInclude(t => t.TipoEmision).ThenInclude(t => t.Alcance)
        .AsSplitQuery();

    public Task<List<Colaboracion>> ListarPorTitularAsync(long idTitular) => Completas().AsNoTracking()
        .Where(c => c.Domicilio.IdUsuario == idTitular)
        .OrderByDescending(c => c.FechaInvitacion).ToListAsync();

    public Task<List<Colaboracion>> ListarPorColaboradorAsync(long idColaborador) => Completas().AsNoTracking()
        .Where(c => c.IdUsuarioColaborador == idColaborador)
        .OrderByDescending(c => c.FechaInvitacion).ToListAsync();

    public Task<Colaboracion?> ObtenerCompletaAsync(long id) => Completas().FirstOrDefaultAsync(c => c.IdColaboracion == id);

    /// <summary>Colaboraciones aceptadas del usuario en ese domicilio que cubren el período.</summary>
    public Task<List<Colaboracion>> ObtenerVigentesAsync(long idColaborador, long idDomicilio, DateOnly periodo) => Ctx.Colaboraciones
        .Include(c => c.TiposEmision)
        .Where(c => c.IdUsuarioColaborador == idColaborador && c.IdDomicilio == idDomicilio
                    && c.Estado == EstadoColaboracion.Aceptada
                    && c.PeriodoDesde <= periodo && (c.PeriodoHasta == null || c.PeriodoHasta >= periodo))
        .ToListAsync();

    public Task<bool> ExisteActivaAsync(long idColaborador, long idDomicilio, long? excluirId = null) => Ctx.Colaboraciones
        .AnyAsync(c => c.IdUsuarioColaborador == idColaborador && c.IdDomicilio == idDomicilio
                       && (c.Estado == EstadoColaboracion.Pendiente || c.Estado == EstadoColaboracion.Aceptada)
                       && (excluirId == null || c.IdColaboracion != excluirId));

    /// <summary>Aportes (cantidad de registros y kg CO2) de cada colaborador por domicilio.</summary>
    public async Task<Dictionary<(long Domicilio, long Usuario), (int Registros, decimal Kg)>> AportesAsync(IReadOnlyCollection<long> idsDomicilios)
    {
        var datos = await Ctx.Registros
            .Where(r => idsDomicilios.Contains(r.IdDomicilio) && r.IdUsuarioCarga != r.Domicilio.IdUsuario)
            .GroupBy(r => new { r.IdDomicilio, r.IdUsuarioCarga })
            .Select(g => new { g.Key.IdDomicilio, g.Key.IdUsuarioCarga, n = g.Count(), kg = g.Sum(r => r.TotalKgCo2) })
            .ToListAsync();
        return datos.ToDictionary(d => (d.IdDomicilio, d.IdUsuarioCarga), d => (d.n, d.kg));
    }

    public void QuitarTipos(IEnumerable<ColaboracionTipoEmision> tipos) => Ctx.ColaboracionTiposEmision.RemoveRange(tipos);
}

public class ReporteRepositorio(HdcDbContext ctx) : Repositorio<Reporte>(ctx)
{
    public Task<List<Reporte>> ListarPorUsuarioAsync(long idUsuario) => Ctx.Reportes.AsNoTracking()
        .Where(r => r.IdUsuario == idUsuario).OrderByDescending(r => r.FechaCreacion).ToListAsync();
}
