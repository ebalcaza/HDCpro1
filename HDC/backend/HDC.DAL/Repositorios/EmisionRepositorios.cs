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

    /// <summary>Registros de los domicilios del usuario más los que él cargó como colaborador.</summary>
    public Task<List<Registro>> ListarVisiblesAsync(long idUsuario, long? idDomicilio) => Completos().AsNoTracking()
        .Where(r => r.Domicilio.IdUsuario == idUsuario || r.IdUsuarioCarga == idUsuario)
        .Where(r => idDomicilio == null || r.IdDomicilio == idDomicilio)
        .OrderByDescending(r => r.Periodo).ThenByDescending(r => r.FechaCreacion)
        .ToListAsync();

    public Task<Registro?> ObtenerCompletoAsync(long idRegistro) =>
        Completos().FirstOrDefaultAsync(r => r.IdRegistro == idRegistro);

    public Task<List<Registro>> ListarPorDomicilioAsync(long idDomicilio) =>
        Ctx.Registros.Where(r => r.IdDomicilio == idDomicilio).ToListAsync();

    public async Task<decimal> SumarPorDomicilioAsync(long idDomicilio) =>
        await Ctx.Registros.Where(r => r.IdDomicilio == idDomicilio).SumAsync(r => (decimal?)r.TotalKgCo2) ?? 0m;

    public void QuitarDetalles(IEnumerable<DetalleRegistro> detalles) => Ctx.DetallesRegistro.RemoveRange(detalles);

    /// <summary>Devuelve cada detalle de emisión con todo su contexto, aplicando los filtros en la base.</summary>
    public Task<List<EmisionPlana>> ObtenerEmisionesAsync(FiltroEmisiones f)
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

        return q.Select(d => new EmisionPlana
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
