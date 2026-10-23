using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace HDC.DAL;

/// <summary>
/// Crea la base de datos si no existe y ejecuta el script embebido Scripts/HDC_base_datos.sql
/// (esquema + datos iniciales + índices).
/// Reintenta la conexión porque el contenedor de SQL Server puede tardar en aceptar conexiones.
/// </summary>
public static partial class InicializadorBaseDatos
{
    /// <summary>
    /// Script único de la base. Es idempotente (IF NOT EXISTS ...), por eso se ejecuta en CADA
    /// arranque: en una base vacía crea todo y en una existente solo agrega lo que falte.
    /// </summary>
    private const string ScriptBase = "HDC_base_datos.sql";

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

        // Todo en una transacción: si un lote falla, la base no queda a medio crear
        await using (var tx = (SqlTransaction)await conexion.BeginTransactionAsync())
        {
            foreach (var lote in DividirEnLotes(LeerScript(ScriptBase)))
            {
                // Crear un índice en una tabla grande puede tardar: se da un tiempo amplio
                await using var cmd = new SqlCommand(lote, conexion, tx) { CommandTimeout = 600 };
                await cmd.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
        }
        logger.LogInformation("Script {Script} aplicado a la base {Base}.", ScriptBase, nombreBase);
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
