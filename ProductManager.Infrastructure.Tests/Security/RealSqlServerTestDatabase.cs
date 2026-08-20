using Microsoft.Data.SqlClient;

namespace ProductManager.Infrastructure.Tests.Security;

/// <summary>
/// Creates and tears down a uniquely-named throwaway database on a real, reachable SQL Server
/// instance so tests can exercise EF Core's relational (<c>Database.IsRelational() == true</c>)
/// code paths — something the InMemory provider used by the rest of the suite can never do.
///
/// Connection resolution order:
/// 1. <c>SQL_TEST_CONNECTION_STRING</c> environment variable, if set (used verbatim, no fallback).
/// 2. The docker-compose <c>sqlserver</c> service (<c>localhost,1433</c>, sa credentials matching
///    <c>.env.example</c>) — started via <c>docker compose up -d sqlserver</c>.
/// 3. SQL Server LocalDB (<c>(localdb)\MSSQLLocalDB</c>), which is relational too and much faster
///    to start locally when Docker isn't available.
///
/// If none of these are reachable, <see cref="TryCreateAsync"/> returns <c>null</c> so callers
/// can skip the dependent tests instead of failing hard.
/// </summary>
internal sealed class RealSqlServerTestDatabase : IAsyncDisposable
{
    private const string DockerComposeSqlServerConnectionString =
        "Server=localhost,1433;User Id=sa;Password=Str0ng!Passw0rd#2026;TrustServerCertificate=True;Encrypt=True;";

    private const string LocalDbConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True;";

    private readonly string _masterConnectionString;
    private readonly string _databaseName;

    public string ConnectionString { get; }

    private RealSqlServerTestDatabase(string masterConnectionString, string databaseName, string databaseConnectionString)
    {
        _masterConnectionString = masterConnectionString;
        _databaseName = databaseName;
        ConnectionString = databaseConnectionString;
    }

    public static async Task<RealSqlServerTestDatabase?> TryCreateAsync()
    {
        var masterConnectionString = await ResolveReachableConnectionStringAsync();
        if (masterConnectionString is null)
        {
            return null;
        }

        var databaseName = $"ProductManagerId_ConcurrencyTests_{Guid.NewGuid():N}";

        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();
        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE [{databaseName}]";
        await createCommand.ExecuteNonQueryAsync();

        var databaseConnectionString = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = databaseName
        }.ConnectionString;

        return new RealSqlServerTestDatabase(masterConnectionString, databaseName, databaseConnectionString);
    }

    private static async Task<string?> ResolveReachableConnectionStringAsync()
    {
        var explicitConnectionString = Environment.GetEnvironmentVariable("SQL_TEST_CONNECTION_STRING");
        if (explicitConnectionString is not null)
        {
            return await CanConnectAsync(explicitConnectionString) ? explicitConnectionString : null;
        }

        if (await CanConnectAsync(DockerComposeSqlServerConnectionString))
        {
            return DockerComposeSqlServerConnectionString;
        }

        if (await CanConnectAsync(LocalDbConnectionString))
        {
            return LocalDbConnectionString;
        }

        return null;
    }

    private static async Task<bool> CanConnectAsync(string connectionString)
    {
        try
        {
            var probeConnectionString = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = 3
            }.ConnectionString;

            await using var connection = new SqlConnection(probeConnectionString);
            await connection.OpenAsync();
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await using var connection = new SqlConnection(_masterConnectionString);
            await connection.OpenAsync();
            await using var dropCommand = connection.CreateCommand();
            dropCommand.CommandText =
                $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}];";
            await dropCommand.ExecuteNonQueryAsync();
        }
        catch (SqlException)
        {
            // Best-effort cleanup: a leftover throwaway database is not worth failing the test run over.
        }
    }
}
