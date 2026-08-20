using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProductManager.Infrastructure.Data;
using ProductManager.Infrastructure.Security;
using ProductManger.Domain.Entities;

namespace ProductManager.Infrastructure.Tests.Security;

/// <summary>
/// Proves that <see cref="ProductIdGenerator"/>'s REAL relational code path — the Serializable
/// transaction, EF Core execution strategy, and <c>WITH (UPDLOCK, ROWLOCK)</c> raw SQL — is safe
/// under real concurrent access. Every other test in the suite runs against EF Core's InMemory
/// provider, where <c>Database.IsRelational()</c> is <c>false</c>, so this locking logic is
/// otherwise never exercised by any automated test.
///
/// Requires a real SQL Server (the docker-compose <c>sqlserver</c> service or LocalDB); see
/// <see cref="RealSqlServerTestDatabase"/> for connection resolution and the README's "Testing"
/// section for how to run it. Skips automatically (rather than failing) when no real SQL Server
/// is reachable.
/// </summary>
public class ProductIdGeneratorConcurrencyTests : IAsyncLifetime
{
    private RealSqlServerTestDatabase? _database;

    public async Task InitializeAsync()
    {
        _database = await RealSqlServerTestDatabase.TryCreateAsync();
        if (_database is null)
        {
            return;
        }

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task GenerateNextIdAsync_UnderConcurrentLoad_AgainstRealSqlServer_ShouldProduceDistinctIds()
    {
        Skip.If(_database is null,
            "No real SQL Server is reachable (tried SQL_TEST_CONNECTION_STRING, the docker-compose " +
            "sqlserver service on localhost,1433, and LocalDB). Start one to run this test — see README.");

        const int startingId = 100_000;
        const int concurrentCallers = 50;

        await using (var seedContext = CreateContext())
        {
            seedContext.Database.IsRelational().Should().BeTrue(
                "this test only proves anything if it exercises the real Serializable/UPDLOCK code path, " +
                "not the InMemory fallback used by the rest of the suite");

            seedContext.ProductIdSequences.Add(new ProductIdSequence { Id = 1, NextProductId = startingId });
            await seedContext.SaveChangesAsync();
        }

        var generatedIds = new ConcurrentBag<int>();

        // Each caller gets its own AppDbContext/ProductIdGenerator instance, mirroring how a real
        // request-scoped DbContext would be handed to concurrent HTTP requests in production —
        // DbContext itself is not thread-safe, so the safety guarantee must come from the
        // database-level Serializable transaction + UPDLOCK/ROWLOCK, not from sharing one context.
        var tasks = Enumerable.Range(0, concurrentCallers).Select(async _ =>
        {
            await using var context = CreateContext();
            var generator = new ProductIdGenerator(context);
            var id = await generator.GenerateNextIdAsync();
            generatedIds.Add(id);
        });

        await Task.WhenAll(tasks);

        generatedIds.Should().HaveCount(concurrentCallers);

        generatedIds.Distinct().Should().HaveCount(concurrentCallers,
            "concurrent callers racing for the same counter row must never be handed the same ID");

        generatedIds.Should().OnlyContain(id => id >= Product.MinId && id <= Product.MaxId,
            "every generated ID must be a valid 6-digit product ID");

        await using var verifyContext = CreateContext();
        var finalSequence = await verifyContext.ProductIdSequences.SingleAsync(s => s.Id == 1);
        finalSequence.NextProductId.Should().Be(startingId + concurrentCallers,
            "the counter must land exactly on start + N with no lost updates and no duplicate claims");
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_database!.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }
}
