using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace HDC.DAL;

/// <summary>
/// Crea la base de datos si no existe y ejecuta los scripts embebidos (esquema + datos iniciales).
/// Reintenta la conexión porque el contenedor de SQL Server puede tardar en aceptar conexiones.
/// </summary>
public static partial class InicializadorBaseDatos
{
    private static readonly string[] Scripts = ["01_esquema.sql", "02_datos_iniciales.sql"];

    public static async Task InicializarAsync(string connectionString, ILogger logger, int intentos = 30)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var nombreBase = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(nombreBase))
            throw new InvalidOperationException("La cadena de conexión no indica la base de datos (Database=...).");

        var master = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" }.ConnectionString;

        for (var intento = 1; ; intento++)
        {
            try
            {
                await using var cn = new SqlConnection(master);
                await cn.OpenAsync();
                await using var cmd = new SqlCommand(
                    $"IF DB_ID(@n) IS NULL CREATE DATABASE {QuoteName(nombreBase)};", cn);
                cmd.Parameters.AddWithValue("@n", nombreBase);
                await cmd.ExecuteNonQueryAsync();
                break;
            }
            catch (SqlException ex) when (intento < intentos)
            {
                logger.LogWarning("SQL Server todavía no está disponible ({Intento}/{Total}): {Mensaje}", intento, intentos, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        await using var conexion = new SqlConnection(connectionString);
        await conexion.OpenAsync();

        await using (var existe = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name = 'ROL'", conexion))
        {
            if ((int)(await existe.ExecuteScalarAsync())! > 0)
            {
                logger.LogInformation("La base {Base} ya tiene el esquema creado.", nombreBase);
                return;
            }
        }

        logger.LogInformation("Creando el esquema de la base {Base}...", nombreBase);
        await using var tx = (SqlTransaction)await conexion.BeginTransactionAsync();
        foreach (var script in Scripts)
        {
            foreach (var lote in DividirEnLotes(LeerScript(script)))
            {
                await using var cmd = new SqlCommand(lote, conexion, tx) { CommandTimeout = 120 };
                await cmd.ExecuteNonQueryAsync();
            }
        }
        await tx.CommitAsync();
        logger.LogInformation("Esquema y datos iniciales creados.");
    }

    private static string LeerScript(string nombre)
    {
        var asm = Assembly.GetExecutingAssembly();
        var recurso = asm.GetManifestResourceNames().Single(n => n.EndsWith(nombre, StringComparison.OrdinalIgnoreCase));
        using var stream = asm.GetManifestResourceStream(recurso)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static IEnumerable<string> DividirEnLotes(string sql) =>
        SeparadorGo().Split(sql).Select(l => l.Trim()).Where(l => l.Length > 0);

    private static string QuoteName(string nombre) => "[" + nombre.Replace("]", "]]") + "]";

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex SeparadorGo();
}
