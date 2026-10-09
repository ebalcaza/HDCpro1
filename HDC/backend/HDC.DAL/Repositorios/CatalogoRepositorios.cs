using HDC.Entity.Entities;
using Microsoft.EntityFrameworkCore;

namespace HDC.DAL.Repositorios;

public class AlcanceRepositorio(HdcDbContext ctx) : Repositorio<Alcance>(ctx)
{
    public Task<List<Alcance>> ListarAsync() => Ctx.Alcances.AsNoTracking()
        .Include(a => a.TiposEmision).OrderBy(a => a.Nombre).ToListAsync();

    public Task<bool> ExisteNombreAsync(string nombre, long? excluirId = null) =>
        Ctx.Alcances.AnyAsync(a => a.Nombre == nombre && (excluirId == null || a.IdAlcance != excluirId));

    public Task<bool> EnUsoAsync(long id) => Ctx.TiposEmision.AnyAsync(t => t.IdAlcance == id);
}

public class UnidadMedidaRepositorio(HdcDbContext ctx) : Repositorio<UnidadMedida>(ctx)
{
    public async Task<List<(UnidadMedida Unidad, int Fuentes)>> ListarConUsoAsync()
    {
        var datos = await Ctx.UnidadesMedida.AsNoTracking()
            .OrderBy(u => u.Nombre)
            .Select(u => new { u, n = Ctx.FuentesEmision.Count(f => f.IdUnidadMedida == u.IdUnidadMedida) })
            .ToListAsync();
        return datos.Select(d => (d.u, d.n)).ToList();
    }

    public Task<bool> ExisteNombreAsync(string nombre, long? excluirId = null) =>
        Ctx.UnidadesMedida.AnyAsync(u => u.Nombre == nombre && (excluirId == null || u.IdUnidadMedida != excluirId));

    public Task<bool> EnUsoAsync(long id) => Ctx.FuentesEmision.AnyAsync(f => f.IdUnidadMedida == id);
}

public class TipoEmisionRepositorio(HdcDbContext ctx) : Repositorio<TipoEmision>(ctx)
{
    public async Task<List<(TipoEmision Tipo, int Fuentes)>> ListarConUsoAsync()
    {
        var datos = await Ctx.TiposEmision.AsNoTracking()
            .Include(t => t.Alcance)
            .OrderBy(t => t.Alcance.Nombre).ThenBy(t => t.Nombre)
            .Select(t => new { t, n = Ctx.FuentesEmision.Count(f => f.IdTipoEmision == t.IdTipoEmision && f.FechaBaja == null) })
            .ToListAsync();
        return datos.Select(d => (d.t, d.n)).ToList();
    }

    public Task<List<TipoEmision>> ObtenerPorIdsAsync(IEnumerable<long> ids) =>
        Ctx.TiposEmision.Include(t => t.Alcance).Where(t => ids.Contains(t.IdTipoEmision)).ToListAsync();

    public async Task<bool> EnUsoAsync(long id) =>
        await Ctx.FuentesEmision.AnyAsync(f => f.IdTipoEmision == id) ||
        await Ctx.ColaboracionTiposEmision.AnyAsync(c => c.IdTipoEmision == id);
}

public class FuenteEmisionRepositorio(HdcDbContext ctx) : Repositorio<FuenteEmision>(ctx)
{
    private IQueryable<FuenteEmision> Completas() => Ctx.FuentesEmision
        .Include(f => f.UnidadMedida)
        .Include(f => f.TipoEmision).ThenInclude(t => t.Alcance);

    public Task<List<FuenteEmision>> ListarAsync(bool incluirBajas) => Completas().AsNoTracking()
        .Where(f => incluirBajas || f.FechaBaja == null)
        .OrderBy(f => f.TipoEmision.Alcance.Nombre).ThenBy(f => f.TipoEmision.Nombre).ThenBy(f => f.Nombre)
        .ToListAsync();

    public Task<FuenteEmision?> ObtenerCompletaAsync(long id) => Completas().FirstOrDefaultAsync(f => f.IdFuenteEmision == id);

    public Task<List<FuenteEmision>> ObtenerPorIdsAsync(IEnumerable<long> ids) =>
        Completas().Where(f => ids.Contains(f.IdFuenteEmision)).ToListAsync();

    public Task<bool> EnUsoAsync(long id) => Ctx.DetallesRegistro.AnyAsync(d => d.IdFuenteEmision == id);
}
