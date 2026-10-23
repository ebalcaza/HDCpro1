using System.Linq.Expressions;

namespace HDC.DAL.Repositorios;

/// <summary>
/// Ayudas para armar consultas paginadas. Todo se traduce a SQL (ORDER BY / OFFSET / FETCH),
/// así la base devuelve solo las filas de la página pedida.
/// </summary>
public static class ExtensionesConsulta
{
    /// <summary>Ordena ascendente o descendente según el parámetro, con una sola llamada.</summary>
    public static IOrderedQueryable<T> OrdenarPor<T, TClave>(this IQueryable<T> q, Expression<Func<T, TClave>> clave, bool asc) =>
        asc ? q.OrderBy(clave) : q.OrderByDescending(clave);

    /// <summary>Saltea las páginas anteriores y toma solo las filas de la página actual.</summary>
    public static IQueryable<T> Paginar<T>(this IQueryable<T> q, int saltear, int tamanio) =>
        q.Skip(saltear).Take(tamanio);
}
