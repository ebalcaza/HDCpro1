using System.Reflection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HDC.DAL;

/// <summary>
/// Crea la base de datos si no existe y ejecuta el script embebido Scripts/HDC_base_datos.sql
/// (esquema + datos iniciales + índices) en PostgreSQL.
/// Reintenta la conexión porque el contenedor de PostgreSQL puede tardar unos segundos en aceptar conexiones.
/// </summary>
public static class InicializadorBaseDatos
{
    /// <summary>
    /// Script único de la base. Es idempotente (CREATE ... IF NOT EXISTS, INSERT ... WHERE NOT EXISTS),
    /// por eso se ejecuta en CADA arranque: en una base vacía crea todo y en una existente solo agrega lo que falte.
    /// </summary>
    private const string ScriptBase = "HDC_base_datos.sql";

    public static async Task InicializarAsync(string connectionString, ILogger logger, int intentos = 30)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var nombreBase = builder.Database;
        if (string.IsNullOrWhiteSpace(nombreBase))
            throw new InvalidOperationException("La cadena de conexión no indica la base de datos (Database=...).");

        // Para crear la base hay que conectarse a otra que siempre exista: "postgres" es la base de mantenimiento
        var mantenimiento = new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres", Pooling = false }.ConnectionString;

        for (var intento = 1; ; intento++)
        {
            try
            {
                await using var cn = new NpgsqlConnection(mantenimiento);
                await cn.OpenAsync();

                // PostgreSQL no tiene "CREATE DATABASE IF NOT EXISTS": se consulta el catálogo pg_database primero
                await using var existe = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @n", cn);
                existe.Parameters.AddWithValue("n", nombreBase);
                if (await existe.ExecuteScalarAsync() is null)
                {
                    // CREATE DATABASE no admite parámetros: el nombre se escapa con comillas dobles
                    await using var crear = new NpgsqlCommand($"CREATE DATABASE {Citar(nombreBase)}", cn);
                    await crear.ExecuteNonQueryAsync();
                    logger.LogInformation("Base de datos {Base} creada.", nombreBase);
                }
                break;
            }
            catch (NpgsqlException ex) when (intento < intentos)
            {
                logger.LogWarning("PostgreSQL todavía no está disponible ({Intento}/{Total}): {Mensaje}", intento, intentos, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        await using var conexion = new NpgsqlConnection(connectionString);
        await conexion.OpenAsync();

        // En PostgreSQL el DDL (CREATE TABLE, CREATE INDEX) también es transaccional:
        // si alguna sentencia falla, se deshace todo y la base no queda a medio crear.
        // A diferencia de SQL Server no hace falta separar el script en lotes con "GO":
        // Npgsql ejecuta varias sentencias separadas por ";" en un solo comando.
        await using (var tx = await conexion.BeginTransactionAsync())
        {
            // Crear un índice en una tabla grande puede tardar: se da un tiempo amplio
            await using var cmd = new NpgsqlCommand(LeerScript(ScriptBase), conexion, tx) { CommandTimeout = 600 };
            await cmd.ExecuteNonQueryAsync();
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

    /// <summary>Equivalente a QUOTENAME de SQL Server: encierra el identificador entre comillas dobles.</summary>
    private static string Citar(string nombre) => "\"" + nombre.Replace("\"", "\"\"") + "\"";
}
