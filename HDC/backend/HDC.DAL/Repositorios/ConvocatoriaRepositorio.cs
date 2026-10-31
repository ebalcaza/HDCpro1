using HDC.Entity.Dtos;
using HDC.Entity.Entities;
using Microsoft.EntityFrameworkCore;

namespace HDC.DAL.Repositorios;

/// <summary>
/// Convocatorias (v2.2): colaboración masiva por código.
/// Una convocatoria puede tener miles de miembros, por eso los listados se paginan en SQL
/// y las acciones masivas se hacen con un único UPDATE (ExecuteUpdate), sin cargar filas en memoria.
/// </summary>
public class ConvocatoriaRepositorio(HdcDbContext ctx) : Repositorio<Convocatoria>(ctx)
{
    /// <summary>Convocatoria con lo necesario para armar el DTO: domicilio, ciudad, titular y tipos de emisión.</summary>
    private IQueryable<Convocatoria> Completas() => Ctx.Convocatorias
        .Include(c => c.Domicilio).ThenInclude(d => d.Ciudad)
        .Include(c => c.Domicilio).ThenInclude(d => d.Usuario).ThenInclude(u => u.Individuo)
        .Include(c => c.Domicilio).ThenInclude(d => d.Usuario).ThenInclude(u => u.Organizacion)
        .Include(c => c.TiposEmision).ThenInclude(t => t.TipoEmision).ThenInclude(t => t.Alcance)
        .AsSplitQuery();

    /// <summary>Convocatorias de los domicilios del titular, de la más nueva a la más vieja.</summary>
    public Task<List<Convocatoria>> ListarPorTitularAsync(long idTitular) => Completas().AsNoTracking()
        .Where(c => c.Domicilio.IdUsuario == idTitular)
        .OrderByDescending(c => c.FechaCreacion)
        .ToListAsync();

    /// <summary>Convocatoria con seguimiento de cambios (para editarla).</summary>
    public Task<Convocatoria?> ObtenerCompletaAsync(long id) => Completas().FirstOrDefaultAsync(c => c.IdConvocatoria == id);

    /// <summary>Busca por código ya normalizado (mayúsculas, sin guion ni espacios).</summary>
    public Task<Convocatoria?> ObtenerPorCodigoAsync(string codigo) => Completas().AsNoTracking().FirstOrDefaultAsync(c => c.Codigo == codigo);

    /// <summary>Para no repetir códigos al generar uno nuevo.</summary>
    public Task<bool> ExisteCodigoAsync(string codigo) => Ctx.Convocatorias.AnyAsync(c => c.Codigo == codigo);

    /// <summary>Una convocatoria con miembros (en cualquier estado) no se elimina: se finaliza.</summary>
    public Task<bool> TieneMiembrosAsync(long id) => Ctx.Colaboraciones.AnyAsync(c => c.IdConvocatoria == id);

    /// <summary>Cantidad de miembros de cada convocatoria por estado: { (idConvocatoria, estado) → cantidad }.</summary>
    public async Task<Dictionary<(long Convocatoria, string Estado), int>> ContarPorEstadoAsync(IReadOnlyCollection<long> ids)
    {
        var datos = await Ctx.Colaboraciones
            .Where(c => c.IdConvocatoria != null && ids.Contains(c.IdConvocatoria.Value))
            .GroupBy(c => new { Id = c.IdConvocatoria!.Value, c.Estado })
            .Select(g => new { g.Key.Id, g.Key.Estado, n = g.Count() })
            .ToListAsync();
        return datos.ToDictionary(d => (d.Id, d.Estado), d => d.n);
    }

    /// <summary>kg CO2 que cargaron los miembros de cada convocatoria en su domicilio (SUM en la base).</summary>
    public async Task<Dictionary<long, decimal>> AportesAsync(IReadOnlyCollection<long> ids)
    {
        var datos = await (
            from c in Ctx.Colaboraciones
            where c.IdConvocatoria != null && ids.Contains(c.IdConvocatoria.Value)
            join r in Ctx.Registros
                on new { c.IdDomicilio, Usuario = c.IdUsuarioColaborador } equals new { r.IdDomicilio, Usuario = r.IdUsuarioCarga }
            group r by c.IdConvocatoria!.Value into g
            select new { Id = g.Key, Kg = g.Sum(r => r.TotalKgCo2) }).ToListAsync();
        return datos.ToDictionary(d => d.Id, d => d.Kg);
    }

    /// <summary>La colaboración de un usuario en una convocatoria (si alguna vez se sumó).</summary>
    public Task<Colaboracion?> ObtenerMiembroAsync(long idConvocatoria, long idUsuario) => Ctx.Colaboraciones
        .FirstOrDefaultAsync(c => c.IdConvocatoria == idConvocatoria && c.IdUsuarioColaborador == idUsuario);

    /// <summary>Miembros que cumplen el filtro de estado y el buscador (nombre, apellido, DNI, razón social o email).</summary>
    public IQueryable<Colaboracion> MiembrosFiltrados(long idConvocatoria, string? estado, string? busqueda)
    {
        var q = Ctx.Colaboraciones.Where(c => c.IdConvocatoria == idConvocatoria);
        if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(c => c.Estado == estado);
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var t = ExtensionesConsulta.PatronContiene(busqueda.Trim());
            q = q.Where(c => EF.Functions.ILike(c.Colaborador.Email, t)
                || (c.Colaborador.Individuo != null && (EF.Functions.ILike(c.Colaborador.Individuo.Nombres, t)
                    || EF.Functions.ILike(c.Colaborador.Individuo.Apellidos, t)
                    || EF.Functions.ILike(c.Colaborador.Individuo.Dni, t)))
                || (c.Colaborador.Organizacion != null && EF.Functions.ILike(c.Colaborador.Organizacion.RazonSocial, t)));
        }
        return q;
    }

    /// <summary>
    /// Página de miembros, ordenada y paginada en SQL. El aporte de cada uno (registros y kg CO2 en el domicilio)
    /// se calcula con subconsultas dentro de la misma consulta: no se traen los registros a memoria.
    /// </summary>
    public async Task<PaginaDto<MiembroConvocatoriaDto>> PaginarMiembrosAsync(long idConvocatoria, long idTitular, FiltroMiembros f)
    {
        var q = MiembrosFiltrados(idConvocatoria, f.Estado, f.Busqueda).AsNoTracking();
        var total = await q.CountAsync();

        var ordenada = (f.Orden?.ToLowerInvariant()) switch
        {
            "colaborador" => q.OrdenarPor(c => c.Colaborador.Individuo != null ? c.Colaborador.Individuo.Apellidos
                : c.Colaborador.Organizacion != null ? c.Colaborador.Organizacion.RazonSocial : c.Colaborador.Email, f.Asc),
            "email" => q.OrdenarPor(c => c.Colaborador.Email, f.Asc),
            "estado" => q.OrdenarPor(c => c.Estado, f.Asc),
            "fechasolicitud" => q.OrdenarPor(c => c.FechaInvitacion, f.Asc),
            "totalkgco2aportado" => q.OrdenarPor(c => Ctx.Registros
                .Where(r => r.IdDomicilio == c.IdDomicilio && r.IdUsuarioCarga == c.IdUsuarioColaborador)
                .Sum(r => (decimal?)r.TotalKgCo2) ?? 0m, f.Asc),
            // Por defecto: las solicitudes más recientes primero
            _ => q.OrderByDescending(c => c.FechaInvitacion),
        };

        var items = await ordenada.ThenBy(c => c.IdColaboracion)
            .Paginar(f.Saltear, f.Tamanio)
            .Select(c => new MiembroConvocatoriaDto(
                c.IdColaboracion,
                c.IdUsuarioColaborador,
                c.Colaborador.Individuo != null ? c.Colaborador.Individuo.Nombres + " " + c.Colaborador.Individuo.Apellidos
                    : c.Colaborador.Organizacion != null ? c.Colaborador.Organizacion.RazonSocial : c.Colaborador.Email,
                c.Colaborador.Email,
                c.Estado,
                c.FechaInvitacion,
                c.FechaRespuesta,
                c.IdUsuarioBaja == idTitular,
                Ctx.Registros.Count(r => r.IdDomicilio == c.IdDomicilio && r.IdUsuarioCarga == c.IdUsuarioColaborador),
                Ctx.Registros.Where(r => r.IdDomicilio == c.IdDomicilio && r.IdUsuarioCarga == c.IdUsuarioColaborador)
                    .Sum(r => (decimal?)r.TotalKgCo2) ?? 0m))
            .ToListAsync();

        return new PaginaDto<MiembroConvocatoriaDto>(items, total);
    }

    /// <summary>
    /// Cambia el estado de los miembros indicados con un único UPDATE en la base: una acción sobre miles
    /// de personas es instantánea. Devuelve cuántas filas cambiaron.
    /// </summary>
    public Task<int> CambiarEstadoAsync(IQueryable<Colaboracion> miembros, string nuevoEstado, long? idUsuarioBaja, DateTime fecha) =>
        miembros.ExecuteUpdateAsync(s => s
            .SetProperty(c => c.Estado, nuevoEstado)
            .SetProperty(c => c.IdUsuarioBaja, idUsuarioBaja)
            .SetProperty(c => c.FechaRespuesta, fecha));

    /// <summary>Copia los períodos de la convocatoria a todos sus miembros (un único UPDATE).</summary>
    public Task<int> SincronizarPeriodosAsync(long idConvocatoria, DateOnly desde, DateOnly? hasta) =>
        Ctx.Colaboraciones.Where(c => c.IdConvocatoria == idConvocatoria)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.PeriodoDesde, desde).SetProperty(c => c.PeriodoHasta, hasta));

    /// <summary>Quita tipos de emisión de la convocatoria (al editarla).</summary>
    public void QuitarTipos(IEnumerable<ConvocatoriaTipoEmision> tipos) => Ctx.ConvocatoriaTiposEmision.RemoveRange(tipos);

    /// <summary>Agrega a una persona que se sumó con el código (se guarda con GuardarAsync).</summary>
    public void AgregarMiembro(Colaboracion c) => Ctx.Colaboraciones.Add(c);
}
