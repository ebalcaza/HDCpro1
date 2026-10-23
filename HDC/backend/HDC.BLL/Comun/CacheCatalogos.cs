using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace HDC.BLL.Comun;

/// <summary>
/// Cache en memoria para datos que casi nunca cambian: catálogos (alcances, unidades, tipos, fuentes)
/// y geografía (provincias, distritos, ciudades). Se registra como Singleton, así la usan todos los requests.
///
/// Cómo funciona:
/// - La primera vez que se pide una lista se consulta la base y el resultado queda guardado.
/// - Las siguientes veces se devuelve desde memoria, sin ir a SQL Server.
/// - Cada dato pertenece a un GRUPO. Cuando el admin modifica algo del grupo se llama a Invalidar(grupo)
///   y se descartan todas sus entradas, para que la próxima consulta lea los datos nuevos.
/// - Además cada entrada vence sola a la hora, por las dudas.
///
/// IMPORTANTE: las listas guardadas se comparten entre requests; quien las recibe solo debe leerlas, nunca modificarlas.
/// </summary>
public class CacheCatalogos(IMemoryCache cache)
{
    public const string GrupoCatalogos = "catalogos";
    public const string GrupoGeo = "geo";

    private static readonly TimeSpan Vencimiento = TimeSpan.FromHours(1);

    // Un "token de cancelación" por grupo: todas las entradas del grupo dependen de él,
    // así cancelar el token borra todas juntas (IMemoryCache no permite borrar por prefijo).
    private readonly Dictionary<string, CancellationTokenSource> _tokens = new();
    private readonly object _candado = new();

    /// <summary>Devuelve el valor guardado o lo calcula con <paramref name="cargar"/> y lo guarda.</summary>
    public async Task<T> ObtenerAsync<T>(string grupo, string clave, Func<Task<T>> cargar)
    {
        var claveCompleta = $"{grupo}:{clave}";
        if (cache.TryGetValue(claveCompleta, out T? guardado) && guardado is not null)
            return guardado;

        var valor = await cargar();
        var opciones = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(Vencimiento)
            .AddExpirationToken(new CancellationChangeToken(TokenDe(grupo).Token));
        cache.Set(claveCompleta, valor, opciones);
        return valor;
    }

    /// <summary>Descarta todo lo guardado del grupo (llamar después de crear, editar o borrar algo del grupo).</summary>
    public void Invalidar(string grupo)
    {
        CancellationTokenSource? anterior;
        lock (_candado)
        {
            _tokens.TryGetValue(grupo, out anterior);
            _tokens[grupo] = new CancellationTokenSource(); // las entradas nuevas usan un token nuevo
        }
        anterior?.Cancel(); // vence todas las entradas que dependían del token anterior
        anterior?.Dispose();
    }

    private CancellationTokenSource TokenDe(string grupo)
    {
        lock (_candado)
        {
            if (!_tokens.TryGetValue(grupo, out var cts))
                _tokens[grupo] = cts = new CancellationTokenSource();
            return cts;
        }
    }
}
