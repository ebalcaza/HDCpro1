using HDC.Entity.Dtos;
using HDC.Entity.Entities;
using Microsoft.EntityFrameworkCore;

namespace HDC.DAL.Repositorios;

/// <summary>Traslados (v2.6, ampliado en v2.8): cada carga genera un registro por mes (ver TrasladoServicio).</summary>
public class CargaTrasladoRepositorio(HdcDbContext ctx) : Repositorio<CargaTraslado>(ctx)
{
    /// <summary>Carga con seguimiento de cambios, con sus medios, registros y detalles para poder reemplazarlos.</summary>
    public Task<CargaTraslado?> ObtenerCompletaAsync(long id) => Ctx.CargasTraslado
        .Include(c => c.Domicilio)
        .Include(c => c.Convocatoria)
        .Include(c => c.ConvocatoriaRol)
        .Include(c => c.Medios).ThenInclude(m => m.FuenteEmision)
        .Include(c => c.Registros).ThenInclude(r => r.Detalles)
        .AsSplitQuery()
        .FirstOrDefaultAsync(c => c.IdCargaTraslado == id);

    public Task<long?> TitularDelDomicilioAsync(long idDomicilio) => Ctx.Domicilios
        .Where(d => d.IdDomicilio == idDomicilio && d.FechaBaja == null)
        .Select(d => (long?)d.IdUsuario)
        .FirstOrDefaultAsync();

    /// <summary>
    /// Roles para elegir al cargar un traslado: los de convocatorias vigentes del domicilio que habilitan traslados.
    /// Con idColaborador, solo los de las convocatorias donde esa persona está aceptada.
    /// </summary>
    public Task<List<RolOpcionDto>> RolesDisponiblesAsync(long idDomicilio, long? idColaborador)
    {
        var q = Ctx.ConvocatoriaRoles.AsNoTracking().Where(r =>
            r.Convocatoria.IdDomicilio == idDomicilio
            && r.Convocatoria.Estado != EstadoConvocatoria.Finalizada
            && r.Convocatoria.TiposEmision.Any(t => t.TipoEmision.ModoCarga == ModoCarga.Traslado));
        if (idColaborador is long id)
            q = q.Where(r => r.Convocatoria.Miembros.Any(m => m.IdUsuarioColaborador == id && m.Estado == EstadoColaboracion.Aceptada));
        return q.OrderBy(r => r.Convocatoria.Nombre).ThenBy(r => r.Nombre)
            .Select(r => new RolOpcionDto(r.IdConvocatoriaRol, r.Nombre, r.Convocatoria.Nombre))
            .ToListAsync();
    }

    /// <summary>Convocatorias con el período de traslados configurado en las que la persona está aceptada.</summary>
    public Task<List<Convocatoria>> ConvocatoriasDisponiblesAsync(long idDomicilio, long idColaborador) => Ctx.Convocatorias.AsNoTracking()
        .Include(c => c.Roles)
        .Where(c => c.IdDomicilio == idDomicilio
            && c.Estado != EstadoConvocatoria.Finalizada
            && c.PeriodoHasta != null && c.SemanasTraslado != null
            && c.TiposEmision.Any(t => t.TipoEmision.ModoCarga == ModoCarga.Traslado)
            && c.Miembros.Any(m => m.IdUsuarioColaborador == idColaborador && m.Estado == EstadoColaboracion.Aceptada))
        .OrderBy(c => c.Nombre)
        .ToListAsync();

    /// <summary>Guarda el último rol usado, para proponerlo en la próxima carga (v2.9). Lo persiste quien guarde la carga.</summary>
    public async Task RecordarRolAsync(long idColaborador, long idConvocatoria, long idRol)
    {
        var miembro = await Ctx.Colaboraciones.FirstOrDefaultAsync(c => c.IdConvocatoria == idConvocatoria && c.IdUsuarioColaborador == idColaborador);
        if (miembro != null) miembro.IdConvocatoriaRol = idRol;
    }

    /// <summary>Meses que la persona ya cargó en una convocatoria, para mostrarle cuáles le faltan.</summary>
    public Task<List<DateOnly>> MesesCargadosAsync(long idUsuario, long idConvocatoria) => Ctx.Registros.AsNoTracking()
        .Where(r => r.IdUsuarioCarga == idUsuario && r.CargaTraslado != null && r.CargaTraslado.IdConvocatoria == idConvocatoria)
        .Select(r => r.Periodo)
        .Distinct()
        .OrderBy(p => p)
        .ToListAsync();

    public Task<List<Colaboracion>> ColaboracionesAceptadasAsync(long idColaborador, long idDomicilio) => Ctx.Colaboraciones.AsNoTracking()
        .Where(c => c.IdUsuarioColaborador == idColaborador && c.IdDomicilio == idDomicilio && c.Estado == EstadoColaboracion.Aceptada)
        .ToListAsync();

    /// <summary>
    /// Evita repetir el mismo medio en el mismo mes dentro de una convocatoria y devuelve el nombre del medio repetido.
    /// Se permiten medios distintos, porque una persona puede combinarlos. Cada convocatoria es independiente: el dueño
    /// decide sus períodos, así que el mismo mes se puede declarar en otra (v2.10). Null = fuera de una convocatoria.
    /// </summary>
    public Task<string?> MedioSolapadoAsync(long idUsuario, long idDomicilio, long? idConvocatoria, IReadOnlyCollection<long> idsFuentes, DateOnly desde, DateOnly hasta, long excluirId) =>
        Ctx.CargasTrasladoMedios
            .Where(m => idsFuentes.Contains(m.IdFuenteEmision)
                && m.CargaTraslado.IdCargaTraslado != excluirId
                && m.CargaTraslado.IdUsuarioCarga == idUsuario && m.CargaTraslado.IdDomicilio == idDomicilio
                && m.CargaTraslado.IdConvocatoria == idConvocatoria
                && m.CargaTraslado.PeriodoDesde <= hasta && m.CargaTraslado.PeriodoHasta >= desde)
            .Select(m => m.FuenteEmision.Nombre)
            .FirstOrDefaultAsync();

    /// <summary>
    /// Quien colabora declara un solo traslado por mes en cada convocatoria (v2.11), sin importar el medio:
    /// para cambiarlo edita la carga, que admite varios medios. Null = fuera de una convocatoria.
    /// </summary>
    public Task<bool> PeriodoCargadoAsync(long idUsuario, long idDomicilio, long? idConvocatoria, DateOnly desde, DateOnly hasta, long excluirId) =>
        Ctx.CargasTraslado.AnyAsync(x => x.IdCargaTraslado != excluirId
            && x.IdUsuarioCarga == idUsuario && x.IdDomicilio == idDomicilio
            && x.IdConvocatoria == idConvocatoria
            && x.PeriodoDesde <= hasta && x.PeriodoHasta >= desde);

    /// <summary>Cargas de la persona en el domicilio: el formulario las usa para bloquear lo ya cargado y ofrecer editarlas.</summary>
    public Task<List<CargaPropiaDto>> CargasPropiasAsync(long idUsuario, long idDomicilio) => Ctx.CargasTraslado.AsNoTracking()
        .Where(x => x.IdUsuarioCarga == idUsuario && x.IdDomicilio == idDomicilio)
        .OrderBy(x => x.PeriodoDesde)
        .Select(x => new CargaPropiaDto(x.IdCargaTraslado, x.IdConvocatoria, x.PeriodoDesde, x.PeriodoHasta))
        .ToListAsync();

    public void QuitarMedio(CargaTrasladoMedio medio) => Ctx.CargasTrasladoMedios.Remove(medio);
}
