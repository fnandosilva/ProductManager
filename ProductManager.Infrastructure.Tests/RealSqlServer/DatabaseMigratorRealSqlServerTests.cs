using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProductManager.Infrastructure.Data;
using ProductManager.Infrastructure.Tests.Security;

namespace ProductManager.Infrastructure.Tests.RealSqlServer;

/// <summary>
/// Proves <see cref="DatabaseMigrator"/>'s two real-SQL-Server-only code paths: the auto-migrate
/// path a Development/Docker-dev environment relies on, and the fail-fast path production is
/// meant to hit when <c>Database:ApplyMigrationsOnStartup=false</c> and the schema is behind —
/// exactly the guard that stops the app from being started against a database no one has
/// migrated yet. EF Core's InMemory provider has no migrations at all, so neither path can be
/// exercised without a real relational engine. Requires a real SQL Server; see
/// <see cref="RealSqlServerTestDatabase"/> and the README's "Testing" section. Skips
/// automatically (rather than failing) when none is reachable.
/// </summary>
public class DatabaseMigratorRealSqlServerTests : IAsyncLifetime
{
    private RealSqlServerTestDatabase? _database;

    public async Task InitializeAsync()
    {
        _database = await RealSqlServerTestDatabase.TryCreateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task EnsureDatabaseReadyAsync_WithMigrationsDisabled_OnAFreshDatabase_ShouldFailFastInsteadOfStarting()
    {
        SkipIfNoRealSqlServer();

        await using var context = CreateContext();
        context.Database.IsRelational().Should().BeTrue(
            "this test only proves anything against a real, unmigrated relational schema, not InMemory");

        var act = async () => await DatabaseMigrator.EnsureDatabaseReadyAsync(context, applyMigrationsOnStartup: false);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pending migration*",
                "a production deployment that forgot to run migrations first must be told loudly, " +
                "not silently start serving requests against the wrong schema");
    }

    [SkippableFact]
    public async Task EnsureDatabaseReadyAsync_WithMigrationsEnabled_OnAFreshDatabase_ShouldApplyThemAndSucceed()
    {
        SkipIfNoRealSqlServer();

        await using var context = CreateContext();

        await DatabaseMigrator.EnsureDatabaseReadyAsync(context, applyMigrationsOnStartup: true);

        (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        (await context.ProductIdSequences.CountAsync()).Should().Be(0,
            "DatabaseMigrator only owns the schema — no rows should exist until DatabaseSeeder runs");
    }

    private void SkipIfNoRealSqlServer()
    {
        Skip.If(_database is null,
            "No real SQL Server is reachable (tried SQL_TEST_CONNECTION_STRING, the docker-compose " +
            "sqlserver service on localhost,1433, and LocalDB). Start one to run this test — see README.");
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_database!.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }
}
