using System.Net.Http.Json;
using System.Text.Json.Serialization;
using HDC.BLL.Comun;
using HDC.DAL.Repositorios;
using HDC.Entity.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HDC.BLL.Servicios;

/// <summary>
/// Datos que no pueden ir en el script SQL: el administrador inicial (contraseña con BCrypt)
/// y la geografía argentina desde la API georef (provincias → departamentos → localidades).
/// </summary>
public class SemillaServicio(
    UsuarioRepositorio usuarios,
    GeoRepositorio geo,
    IHttpClientFactory httpFactory,
    IConfiguration config,
    ILogger<SemillaServicio> logger)
{
    public const string ClienteGeoref = "georef";
    private const int TamanioPagina = 5000;

    public async Task CrearAdministradorAsync()
    {
        if (await usuarios.HayAdministradorAsync(RolesSistema.Administrador)) return;

        var email = (config["Admin:Email"] ?? "admin@hdc.com").Trim().ToLowerInvariant();
        var contrasena = config["Admin:Contrasena"];
        if (string.IsNullOrWhiteSpace(contrasena))
        {
            logger.LogWarning("No se configuró Admin:Contrasena; no se crea el administrador inicial.");
            return;
        }

        usuarios.Agregar(new Usuario
        {
            IdRol = RolesSistema.Administrador,
            Email = email,
            Contrasena = AuthServicio.HashContrasena(contrasena),
            FechaAlta = DateTime.Now
        });
        await usuarios.GuardarAsync();
        logger.LogInformation("Administrador inicial creado: {Email}", email);
    }

    /// <summary>Carga provincias, distritos y ciudades si la tabla CIUDAD está vacía.</summary>
    public async Task<int> SincronizarGeorefAsync(CancellationToken ct = default)
    {
        if (await geo.HayDatosAsync()) return 0;

        logger.LogInformation("Descargando geografía desde la API georef...");
        var http = httpFactory.CreateClient(ClienteGeoref);

        var provincias = (await DescargarAsync<ProvinciaGeoref>(http, "provincias", "id,nombre", r => r.Provincias, ct))
            .Select(p => new Provincia { IdProvincia = long.Parse(p.Id), Nombre = p.Nombre })
            .ToList();

        var distritos = (await DescargarAsync<DepartamentoGeoref>(http, "departamentos", "id,nombre,provincia.id", r => r.Departamentos, ct))
            .Where(d => d.ProvinciaId != null)
            .Select(d => new Distrito { IdDistrito = long.Parse(d.Id), IdProvincia = long.Parse(d.ProvinciaId!), Nombre = d.Nombre })
            .ToList();

        var localidades = await DescargarAsync<LocalidadGeoref>(http, "localidades", "id,nombre,departamento.id,provincia.id", r => r.Localidades, ct);

        // Localidades sin departamento: se agrupan en un distrito "Sin departamento" por provincia
        var idsDistritos = distritos.Select(d => d.IdDistrito).ToHashSet();
        var ciudades = new List<Ciudad>();
        foreach (var l in localidades)
        {
            long idDistrito;
            if (l.DepartamentoId != null && idsDistritos.Contains(long.Parse(l.DepartamentoId)))
            {
                idDistrito = long.Parse(l.DepartamentoId);
            }
            else
            {
                var idProv = long.Parse(l.ProvinciaId!);
                idDistrito = idProv * 1000 + 999;
                if (idsDistritos.Add(idDistrito))
                    distritos.Add(new Distrito { IdDistrito = idDistrito, IdProvincia = idProv, Nombre = "Sin departamento" });
            }
            ciudades.Add(new Ciudad { IdCiudad = long.Parse(l.Id), IdDistrito = idDistrito, Nombre = l.Nombre });
        }

        await geo.ReemplazarTodoAsync(provincias, distritos, ciudades);
        logger.LogInformation("Georef: {P} provincias, {D} distritos, {C} ciudades cargadas.", provincias.Count, distritos.Count, ciudades.Count);
        return ciudades.Count;
    }

    private static async Task<List<T>> DescargarAsync<T>(
        HttpClient http, string recurso, string campos, Func<RespuestaGeoref, List<T>?> lista, CancellationToken ct)
    {
        var res = new List<T>();
        var inicio = 0;
        while (true)
        {
            var url = $"{recurso}?campos={campos}&max={TamanioPagina}&inicio={inicio}&aplanar=true";
            var r = await http.GetFromJsonAsync<RespuestaGeoref>(url, ct) ?? throw new InvalidOperationException($"Respuesta vacía de georef ({recurso})");
            var pagina = lista(r) ?? [];
            res.AddRange(pagina);
            inicio += pagina.Count;
            if (pagina.Count == 0 || inicio >= r.Total) return res;
        }
    }

    private class RespuestaGeoref
    {
        [JsonPropertyName("total")] public int Total { get; set; }
        [JsonPropertyName("provincias")] public List<ProvinciaGeoref>? Provincias { get; set; }
        [JsonPropertyName("departamentos")] public List<DepartamentoGeoref>? Departamentos { get; set; }
        [JsonPropertyName("localidades")] public List<LocalidadGeoref>? Localidades { get; set; }
    }

    private record ProvinciaGeoref([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("nombre")] string Nombre);

    private record DepartamentoGeoref(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("nombre")] string Nombre,
        [property: JsonPropertyName("provincia_id")] string? ProvinciaId);

    private record LocalidadGeoref(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("nombre")] string Nombre,
        [property: JsonPropertyName("departamento_id")] string? DepartamentoId,
        [property: JsonPropertyName("provincia_id")] string? ProvinciaId);
}
