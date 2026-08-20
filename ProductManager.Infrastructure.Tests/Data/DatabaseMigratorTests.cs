using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProductManager.Infrastructure.Data;

namespace ProductManager.Infrastructure.Tests.Data;

public class DatabaseMigratorTests
{
    [Fact]
    public async Task EnsureDatabaseReadyAsync_OnNonRelationalProvider_ShouldEnsureCreatedRegardlessOfFlag()
    {
        using var context = TestDbContextFactory.Create();

        await DatabaseMigrator.EnsureDatabaseReadyAsync(context, applyMigrationsOnStartup: false);

        // No exception, and the "database" is usable — InMemory has no migrations to apply or
        // skip, so the applyMigrationsOnStartup flag (which only matters for a real relational
        // engine) must not change this behavior.
        (await context.ProductIdSequences.CountAsync()).Should().Be(0);
    }
}
