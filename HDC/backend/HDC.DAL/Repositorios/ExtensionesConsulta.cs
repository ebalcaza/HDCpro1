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

    /// <summary>
    /// Arma el patrón "%texto%" para EF.Functions.ILike (búsqueda "contiene" sin distinguir mayúsculas).
    /// En PostgreSQL LIKE distingue mayúsculas; ILIKE no. Se escapan los comodines % y _ y la barra
    /// invertida (carácter de escape por defecto) para que el usuario los busque como texto literal.
    /// </summary>
    public static string PatronContiene(string texto) =>
        "%" + texto.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";

    /// <summary>Saltea las páginas anteriores y toma solo las filas de la página actual.</summary>
    public static IQueryable<T> Paginar<T>(this IQueryable<T> q, int saltear, int tamanio) =>
        q.Skip(saltear).Take(tamanio);
}
