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
        .Include(c => c.Roles)
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

    /// <summary>
    /// Borra la convocatoria con todo lo que nació de ella (v2.11): registros, cargas de traslado, miembros, roles y tipos.
    /// Va en una transacción, para no dejar el total del domicilio desactualizado si algo falla. EnableRetryOnFailure
    /// exige envolverla en la estrategia de ejecución.
    /// </summary>
    public Task EliminarConAportesAsync(Convocatoria c)
    {
        var id = c.IdConvocatoria;
        var idDomicilio = c.IdDomicilio;
        return Ctx.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await Ctx.Database.BeginTransactionAsync();

            // Una carga libre puede tener solo algunos meses asignados a esta convocatoria: se borra únicamente si queda vacía
            var cargasAfectadas = await Ctx.Registros
                .Where(r => r.IdConvocatoria == id && r.IdCargaTraslado != null)
                .Select(r => r.IdCargaTraslado!.Value).Distinct().ToListAsync();
            await Ctx.Registros.Where(r => r.IdConvocatoria == id).ExecuteDeleteAsync();
            await Ctx.CargasTraslado
                .Where(x => x.IdConvocatoria == id || (cargasAfectadas.Contains(x.IdCargaTraslado) && !x.Registros.Any()))
                .ExecuteDeleteAsync();
            await Ctx.Colaboraciones.Where(m => m.IdConvocatoria == id).ExecuteDeleteAsync();

            Ctx.Convocatorias.Remove(c); // roles y tipos se borran en cascada
            await Ctx.SaveChangesAsync();

            // Lo borrado deja de sumar a la huella del domicilio
            var total = await Ctx.Registros.Where(r => r.IdDomicilio == idDomicilio).SumAsync(r => (decimal?)r.TotalKgCo2) ?? 0m;
            await Ctx.Domicilios.Where(d => d.IdDomicilio == idDomicilio).ExecuteUpdateAsync(s => s.SetProperty(d => d.TotalKgCo2, total));

            await tx.CommitAsync();
        });
    }

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

    /// <summary>kg CO2 cargados en cada convocatoria (SUM en la base). Solo cuenta lo cargado en esa convocatoria (v2.10).</summary>
    public async Task<Dictionary<long, decimal>> AportesAsync(IReadOnlyCollection<long> ids)
    {
        var datos = await Ctx.Registros
            .Where(r => r.IdConvocatoria != null && ids.Contains(r.IdConvocatoria.Value))
            .GroupBy(r => r.IdConvocatoria!.Value)
            .Select(g => new { Id = g.Key, Kg = g.Sum(r => r.TotalKgCo2) })
            .ToListAsync();
        return datos.ToDictionary(d => d.Id, d => d.Kg);
    }

    /// <summary>
    /// Por cada rol: personas y kg CO2 de sus traslados (todo se resuelve en la base).
    /// Cuentan quienes lo eligieron al sumarse, aunque todavía no hayan cargado, y quienes ya cargaron con ese rol (v2.10).
    /// </summary>
    public async Task<Dictionary<long, (int Colaboradores, decimal Kg)>> ResultadosPorRolAsync(IReadOnlyCollection<long> idsRoles)
    {
        var personas = await Ctx.Colaboraciones
            .Where(c => c.Estado == EstadoColaboracion.Aceptada && c.IdConvocatoriaRol != null && idsRoles.Contains(c.IdConvocatoriaRol.Value))
            .Select(c => new { Rol = c.IdConvocatoriaRol!.Value, Usuario = c.IdUsuarioColaborador })
            .Union(Ctx.CargasTraslado
                .Where(x => x.IdConvocatoriaRol != null && idsRoles.Contains(x.IdConvocatoriaRol.Value))
                .Select(x => new { Rol = x.IdConvocatoriaRol!.Value, Usuario = x.IdUsuarioCarga }))
            .GroupBy(x => x.Rol)
            .Select(g => new { Id = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Cantidad);
        var kilos = await Ctx.Registros
            .Where(r => r.CargaTraslado != null && r.CargaTraslado.IdConvocatoriaRol != null && idsRoles.Contains(r.CargaTraslado.IdConvocatoriaRol.Value))
            .GroupBy(r => r.CargaTraslado!.IdConvocatoriaRol!.Value)
            .Select(g => new { Id = g.Key, Kg = g.Sum(r => r.TotalKgCo2) })
            .ToDictionaryAsync(x => x.Id, x => x.Kg);
        return idsRoles.ToDictionary(id => id, id => (personas.GetValueOrDefault(id), kilos.GetValueOrDefault(id)));
    }

    /// <summary>
    /// Detalle de aportes (v2.8): una fila por fuente y por mes, de todas las personas de la convocatoria o de una sola.
    /// Se ordena y pagina en SQL, porque con miles de colaboradores puede haber muchas filas.
    /// </summary>
    public async Task<PaginaDto<AporteConvocatoriaDto>> PaginarAportesAsync(Convocatoria c, FiltroAportes f)
    {
        var idConvocatoria = c.IdConvocatoria;
        var q = Ctx.DetallesRegistro.AsNoTracking().Where(d => d.Registro.IdConvocatoria == idConvocatoria);
        if (f.IdUsuario is long idUsuario) q = q.Where(d => d.Registro.IdUsuarioCarga == idUsuario);
        if (!string.IsNullOrWhiteSpace(f.Busqueda))
        {
            var t = ExtensionesConsulta.PatronContiene(f.Busqueda.Trim());
            q = q.Where(d => EF.Functions.ILike(d.Registro.UsuarioCarga.Email, t)
                || EF.Functions.ILike(d.FuenteEmision.Nombre, t)
                || (d.Registro.UsuarioCarga.Individuo != null && (EF.Functions.ILike(d.Registro.UsuarioCarga.Individuo.Nombres, t)
                    || EF.Functions.ILike(d.Registro.UsuarioCarga.Individuo.Apellidos, t)))
                || (d.Registro.UsuarioCarga.Organizacion != null && EF.Functions.ILike(d.Registro.UsuarioCarga.Organizacion.RazonSocial, t))
                || (d.Registro.CargaTraslado != null && d.Registro.CargaTraslado.ConvocatoriaRol != null
                    && EF.Functions.ILike(d.Registro.CargaTraslado.ConvocatoriaRol.Nombre, t)));
        }
        var total = await q.CountAsync();

        var ordenada = (f.Orden?.ToLowerInvariant()) switch
        {
            "colaborador" => q.OrdenarPor(d => d.Registro.UsuarioCarga.Individuo != null ? d.Registro.UsuarioCarga.Individuo.Apellidos
                : d.Registro.UsuarioCarga.Organizacion != null ? d.Registro.UsuarioCarga.Organizacion.RazonSocial : d.Registro.UsuarioCarga.Email, f.Asc),
            "fuente" => q.OrdenarPor(d => d.FuenteEmision.Nombre, f.Asc),
            "kgco2" => q.OrdenarPor(d => d.KgCo2, f.Asc),
            "periodo" => q.OrdenarPor(d => d.Registro.Periodo, f.Asc),
            _ => q.OrderByDescending(d => d.Registro.Periodo),
        };

        var items = await ordenada.ThenBy(d => d.IdDetalle)
            .Paginar(f.Saltear, f.Tamanio)
            .Select(d => new AporteConvocatoriaDto(
                d.IdDetalle,
                d.IdRegistro,
                d.Registro.IdUsuarioCarga,
                d.Registro.UsuarioCarga.Individuo != null ? d.Registro.UsuarioCarga.Individuo.Nombres + " " + d.Registro.UsuarioCarga.Individuo.Apellidos
                    : d.Registro.UsuarioCarga.Organizacion != null ? d.Registro.UsuarioCarga.Organizacion.RazonSocial : d.Registro.UsuarioCarga.Email,
                d.Registro.UsuarioCarga.Email,
                d.Registro.CargaTraslado != null && d.Registro.CargaTraslado.ConvocatoriaRol != null ? d.Registro.CargaTraslado.ConvocatoriaRol.Nombre : null,
                d.Registro.Periodo,
                d.FuenteEmision.Nombre,
                d.FuenteEmision.TipoEmision.Nombre,
                d.Consumo,
                d.FuenteEmision.UnidadMedida.Nombre,
                Ctx.CargasTrasladoMedios.Where(m => m.IdCargaTraslado == d.Registro.IdCargaTraslado && m.IdFuenteEmision == d.IdFuenteEmision).Select(m => (decimal?)m.DistanciaKm).FirstOrDefault(),
                Ctx.CargasTrasladoMedios.Where(m => m.IdCargaTraslado == d.Registro.IdCargaTraslado && m.IdFuenteEmision == d.IdFuenteEmision).Select(m => (decimal?)m.DiasSemana).FirstOrDefault(),
                d.ValorFactorEmision,
                d.KgCo2,
                d.Registro.FechaCreacion))
            .ToListAsync();
        return new PaginaDto<AporteConvocatoriaDto>(items, total);
    }

    /// <summary>Un rol elegido en algún traslado no se puede quitar: se perdería el dato de esas cargas.</summary>
    public Task<bool> RolEnUsoAsync(long idRol) => Ctx.CargasTraslado.AnyAsync(c => c.IdConvocatoriaRol == idRol);

    public void QuitarRol(ConvocatoriaRol rol) => Ctx.ConvocatoriaRoles.Remove(rol);

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
                .Where(r => r.IdConvocatoria == idConvocatoria && r.IdUsuarioCarga == c.IdUsuarioColaborador)
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
                Ctx.Registros.Count(r => r.IdConvocatoria == idConvocatoria && r.IdUsuarioCarga == c.IdUsuarioColaborador),
                Ctx.Registros.Where(r => r.IdConvocatoria == idConvocatoria && r.IdUsuarioCarga == c.IdUsuarioColaborador)
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
