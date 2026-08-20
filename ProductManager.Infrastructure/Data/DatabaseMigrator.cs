using Microsoft.EntityFrameworkCore;

namespace ProductManager.Infrastructure.Data;

/// <summary>
/// Owns exactly one job: getting the database schema to the state the current model expects.
/// Deliberately separate from <see cref="Seed.DatabaseSeeder"/>, which owns the data that goes
/// into that schema — bundling "apply schema changes" and "insert rows" into one method made it
/// impossible to run (or skip) either independently, which matters a lot once migrations stop
/// being something you want to run unattended on every app startup in production.
/// </summary>
public static class DatabaseMigrator
{
    /// <param name="applyMigrationsOnStartup">
    /// When <c>true</c> (the local/Docker-dev default), pending migrations are applied
    /// automatically on startup — convenient, but not something a real production deployment
    /// should do: multiple instances starting concurrently can race to apply the same migration,
    /// and it couples "deploy new code" to "change the schema" with no separate review/rollback
    /// point. Production should set this to <c>false</c> and apply migrations as an explicit,
    /// controlled step before the new app version is deployed (see README.md), in which case this
    /// method fails fast instead of starting the app against a schema it doesn't expect.
    /// </param>
    public static async Task EnsureDatabaseReadyAsync(
        AppDbContext context,
        bool applyMigrationsOnStartup,
        CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsRelational())
        {
            // Tests run on EF Core's InMemory provider, which has no migrations at all.
            await context.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        if (applyMigrationsOnStartup)
        {
            await context.Database.MigrateAsync(cancellationToken);
            return;
        }

        var pendingMigrations = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pendingMigrations.Count > 0)
        {
            throw new InvalidOperationException(
                $"Database schema is out of date: {pendingMigrations.Count} pending migration(s) " +
                $"({string.Join(", ", pendingMigrations)}) and Database:ApplyMigrationsOnStartup is false. " +
                "Apply migrations as an explicit deploy step before starting the app " +
                "(dotnet ef database update --project ProductManager.Infrastructure --startup-project " +
                "ProductManager.WebAPI), or set Database:ApplyMigrationsOnStartup=true for local/dev " +
                "convenience. See README.md.");
        }
    }
}
