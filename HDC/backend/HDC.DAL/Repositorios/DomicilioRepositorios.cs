using HDC.Entity.Entities;
using Microsoft.EntityFrameworkCore;

namespace HDC.DAL.Repositorios;

public class GeoRepositorio(HdcDbContext ctx) : Repositorio<Provincia>(ctx)
{
    public Task<List<Provincia>> ListarProvinciasAsync() =>
        Ctx.Provincias.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync();

    public Task<List<Distrito>> ListarDistritosAsync(long idProvincia) =>
        Ctx.Distritos.AsNoTracking().Where(d => d.IdProvincia == idProvincia).OrderBy(d => d.Nombre).ToListAsync();

    public Task<List<Ciudad>> ListarCiudadesAsync(long idDistrito) =>
        Ctx.Ciudades.AsNoTracking().Where(c => c.IdDistrito == idDistrito).OrderBy(c => c.Nombre).ToListAsync();

    public Task<Ciudad?> ObtenerCiudadAsync(long idCiudad) =>
        Ctx.Ciudades.Include(c => c.Distrito).ThenInclude(d => d.Provincia).FirstOrDefaultAsync(c => c.IdCiudad == idCiudad);

    public Task<bool> HayDatosAsync() => Ctx.Ciudades.AnyAsync();

    public async Task ReemplazarTodoAsync(IEnumerable<Provincia> provincias, IEnumerable<Distrito> distritos, IEnumerable<Ciudad> ciudades)
    {
        Ctx.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            Ctx.Provincias.AddRange(provincias);
            Ctx.Distritos.AddRange(distritos);
            Ctx.Ciudades.AddRange(ciudades);
            await Ctx.SaveChangesAsync();
        }
        finally
        {
            Ctx.ChangeTracker.AutoDetectChangesEnabled = true;
            Ctx.ChangeTracker.Clear();
        }
    }
}

public class DomicilioRepositorio(HdcDbContext ctx) : Repositorio<Domicilio>(ctx)
{
    private IQueryable<Domicilio> Completos() => Ctx.Domicilios
        .Include(d => d.Ciudad).ThenInclude(c => c.Distrito).ThenInclude(di => di.Provincia)
        .Include(d => d.Fuentes).ThenInclude(f => f.FuenteEmision).ThenInclude(f => f.UnidadMedida)
        .Include(d => d.Fuentes).ThenInclude(f => f.FuenteEmision).ThenInclude(f => f.TipoEmision).ThenInclude(t => t.Alcance)
        .AsSplitQuery();

    public Task<List<Domicilio>> ListarPorUsuarioAsync(long idUsuario, bool incluirBajas = false) => Completos().AsNoTracking()
        .Where(d => d.IdUsuario == idUsuario && (incluirBajas || d.FechaBaja == null))
        .OrderBy(d => d.FechaAlta)
        .ToListAsync();

    public Task<Domicilio?> ObtenerCompletoAsync(long idDomicilio) =>
        Completos().FirstOrDefaultAsync(d => d.IdDomicilio == idDomicilio);

    public async Task<Dictionary<long, (int Registros, int Colaboradores)>> ConteosAsync(IReadOnlyCollection<long> ids)
    {
        var registros = await Ctx.Registros.Where(r => ids.Contains(r.IdDomicilio))
            .GroupBy(r => r.IdDomicilio).Select(g => new { g.Key, n = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.n);
        var colaboradores = await Ctx.Colaboraciones
            .Where(c => ids.Contains(c.IdDomicilio) && c.Estado == EstadoColaboracion.Aceptada)
            .GroupBy(c => c.IdDomicilio).Select(g => new { g.Key, n = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.n);
        return ids.ToDictionary(id => id, id => (registros.GetValueOrDefault(id), colaboradores.GetValueOrDefault(id)));
    }

    public void QuitarFuentes(IEnumerable<DomicilioFuente> fuentes) => Ctx.DomicilioFuentes.RemoveRange(fuentes);
}
