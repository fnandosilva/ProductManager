using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProductManager.Infrastructure.Data;
using ProductManager.Infrastructure.Products;
using ProductManager.Infrastructure.Tests.Security;
using ProductManager.Domain.Entities;

namespace ProductManager.Infrastructure.Tests.RealSqlServer;

/// <summary>
/// Proves <see cref="ProductRepository.DecrementStockAsync"/> and
/// <see cref="ProductRepository.AddToStockAsync"/> are safe under real concurrent access — the
/// same category of gap the <c>ProductIdGeneratorConcurrencyTests</c> closed for ID allocation,
/// but for stock. Before these repository methods existed, the command handlers did a plain
/// get-then-update: two concurrent requests could both read the same "before" stock and one
/// update would silently overwrite the other (a lost update), or — worse, for an inventory API —
/// both could see "enough" stock available and both succeed, selling more than was ever in stock.
/// EF Core's InMemory provider has no real transactions or row locks, so it can never exercise
/// (or disprove) either failure mode; only a real relational engine can. Requires a real SQL
/// Server; see <see cref="RealSqlServerTestDatabase"/> and the README's "Testing" section. Skips
/// automatically (rather than failing) when none is reachable.
/// </summary>
public class ProductRepositoryStockConcurrencyRealSqlServerTests : IAsyncLifetime
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
    public async Task DecrementStockAsync_UnderConcurrentLoad_WithEnoughStockForEveryCaller_ShouldLoseNoUpdates()
    {
        SkipIfNoRealSqlServer();

        const int startingStock = 1_000;
        const int concurrentCallers = 50;

        await SeedProductAsync(startingStock);

        var tasks = Enumerable.Range(0, concurrentCallers).Select(async _ =>
        {
            await using var context = CreateContext();
            await new ProductRepository(context).DecrementStockAsync(100_001, 1);
        });

        await Task.WhenAll(tasks);

        await using var verifyContext = CreateContext();
        var product = await verifyContext.Products.SingleAsync(p => p.Id == 100_001);
        product.Stock.Should().Be(startingStock - concurrentCallers,
            "every one of the 50 concurrent single-unit decrements must be reflected — a plain " +
            "get-then-update would let some of them silently overwrite each other instead");
    }

    [SkippableFact]
    public async Task DecrementStockAsync_UnderConcurrentLoad_WithNotEnoughStockForEveryCaller_ShouldNeverOversell()
    {
        SkipIfNoRealSqlServer();

        const int startingStock = 10;
        const int concurrentCallers = 20;

        await SeedProductAsync(startingStock);

        var succeeded = new ConcurrentBag<bool>();

        var tasks = Enumerable.Range(0, concurrentCallers).Select(async _ =>
        {
            await using var context = CreateContext();
            try
            {
                await new ProductRepository(context).DecrementStockAsync(100_001, 1);
                succeeded.Add(true);
            }
            catch (InvalidOperationException)
            {
                succeeded.Add(false);
            }
        });

        await Task.WhenAll(tasks);

        succeeded.Count(x => x).Should().Be(startingStock,
            "exactly as many decrements as there was stock for must succeed — no more");
        succeeded.Count(x => !x).Should().Be(concurrentCallers - startingStock,
            "everyone else must see 'insufficient stock', not a corrupted/negative result");

        await using var verifyContext = CreateContext();
        var product = await verifyContext.Products.SingleAsync(p => p.Id == 100_001);
        product.Stock.Should().Be(0,
            "stock must land exactly on zero — never negative (oversold) and never left with " +
            "unclaimed units due to a lost update");
    }

    [SkippableFact]
    public async Task AddToStockAsync_UnderConcurrentLoad_ShouldLoseNoUpdates()
    {
        SkipIfNoRealSqlServer();

        const int concurrentCallers = 50;

        await SeedProductAsync(startingStock: 0);

        var tasks = Enumerable.Range(0, concurrentCallers).Select(async _ =>
        {
            await using var context = CreateContext();
            await new ProductRepository(context).AddToStockAsync(100_001, 1);
        });

        await Task.WhenAll(tasks);

        await using var verifyContext = CreateContext();
        var product = await verifyContext.Products.SingleAsync(p => p.Id == 100_001);
        product.Stock.Should().Be(concurrentCallers,
            "every one of the 50 concurrent single-unit additions must be reflected");
    }

    private async Task SeedProductAsync(int startingStock)
    {
        await using var context = CreateContext();
        context.Database.IsRelational().Should().BeTrue(
            "this test only proves anything against a real locked read-modify-write, not InMemory's unsynchronized one");

        context.Products.Add(Product.Create(100_001, "Concurrency Test Product", null, 1m, startingStock));
        await context.SaveChangesAsync();
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
