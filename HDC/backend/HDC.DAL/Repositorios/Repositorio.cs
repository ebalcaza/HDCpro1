namespace HDC.DAL.Repositorios;

/// <summary>
/// Operaciones comunes. Todos los repositorios comparten el mismo DbContext por request,
/// así que GuardarAsync persiste todos los cambios pendientes como una unidad de trabajo.
/// </summary>
public class Repositorio<T>(HdcDbContext ctx) where T : class
{
    protected readonly HdcDbContext Ctx = ctx;

    public async Task<T?> ObtenerPorIdAsync(params object[] ids) => await Ctx.Set<T>().FindAsync(ids);

    public void Agregar(T entidad) => Ctx.Set<T>().Add(entidad);

    public void AgregarRango(IEnumerable<T> entidades) => Ctx.Set<T>().AddRange(entidades);

    public void Eliminar(T entidad) => Ctx.Set<T>().Remove(entidad);

    public void EliminarRango(IEnumerable<T> entidades) => Ctx.Set<T>().RemoveRange(entidades);

    public Task<int> GuardarAsync(CancellationToken ct = default) => Ctx.SaveChangesAsync(ct);
}
