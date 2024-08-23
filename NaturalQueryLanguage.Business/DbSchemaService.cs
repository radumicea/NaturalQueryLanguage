using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using NaturalQueryLanguage.Clients;

namespace NaturalQueryLanguage.Business;

// Use user id or something from JWT and put file at path like $"{userId}/{dbName}"
public class DbSchemaService(StorageClient storageClient, IConfiguration configuration)
{
    private readonly string _containerName = configuration["Storage:ContainerName"]!;

    public async Task CreateOrUpdate(string provider, string connectionString)
    {
        switch (provider)
        {
            case DbProviders.SQL_Server:
                {
                    await DumpSqlServerSchemaAsync(connectionString);
                    break;
                }

            default:
                {
                    throw new ArgumentException(null, nameof(provider));
                }
        };
    }

    public async Task<Stream> Get(string connectionString)
    {
        var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
        return await storageClient.OpenReadAsync(_containerName, sqlConnectionStringBuilder.InitialCatalog);
    }

    private async Task DumpSqlServerSchemaAsync(string connectionString)
    {
        var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);

        await using var sqlConnection = new SqlConnection(connectionString);
        var serverConnection = new ServerConnection(sqlConnection);

        var server = new Server(serverConnection);
        var database = server.Databases[sqlConnectionStringBuilder.InitialCatalog];

        var scriptingOptions = new ScriptingOptions
        {
            ClusteredIndexes = true,
            Default = true,
            FullTextIndexes = true,
            Indexes = true,
            NonClusteredIndexes = true,
            DriAll = true
        };

        await using var outputStream = await storageClient.OpenWriteAsync(_containerName, sqlConnectionStringBuilder.InitialCatalog);
        await using var streamWriter = new StreamWriter(outputStream);

        foreach (Table table in database.Tables)
        {
            if (!table.IsSystemObject)
            {
                foreach (string line in table.Script(scriptingOptions))
                {
                    await streamWriter.WriteLineAsync(line);
                }
            }
        }

        foreach (View view in database.Views)
        {
            if (!view.IsSystemObject)
            {
                foreach (string line in view.Script(scriptingOptions))
                {
                    await streamWriter.WriteLineAsync(line);
                }
            }
        }

        foreach (StoredProcedure storedProcedure in database.StoredProcedures)
        {
            if (!storedProcedure.IsSystemObject)
            {
                foreach (string line in storedProcedure.Script(scriptingOptions))
                {
                    await streamWriter.WriteLineAsync(line);
                }
            }
        }
    }
}

public static class DbProviders
{
    public const string SQL_Server = "SQL Server";
    public const string Oracle = "Oracle";
    public const string PostgreSQL = "PostgreSQL";
    public const string MySQL = "MySQL";
    public const string MariaDB = "MySQL";
    public const string SQLite = "SQLite";
}
