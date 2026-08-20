using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProductManager.Infrastructure.Data;
using ProductManager.Infrastructure.Products;
using ProductManager.Infrastructure.Tests.Security;
using ProductManager.Domain.Entities;

namespace ProductManager.Infrastructure.Tests.RealSqlServer;

/// <summary>
/// Proves behaviors of <c>Products</c>' real column mapping (<c>decimal(18,2)</c>) and the raw
/// <c>LIKE</c> translation in <see cref="ProductRepository.SearchByNameAsync"/> that the InMemory
/// provider — used by every other repository test — cannot exercise at all: InMemory stores
/// whatever CLR <see cref="decimal"/> value you give it verbatim (no column precision/scale is
/// ever applied), and its <c>LIKE</c> emulation does not go through SQL Server's actual wildcard
/// parser. Requires a real SQL Server; see <see cref="RealSqlServerTestDatabase"/> and the
/// README's "Testing" section. Skips automatically (rather than failing) when none is reachable.
/// </summary>
public class ProductRepositoryRealSqlServerTests : IAsyncLifetime
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
    public async Task Price_WithMoreThanTwoDecimalPlaces_ShouldBeSilentlyRoundedByTheDecimal18_2Column()
    {
        SkipIfNoRealSqlServer();

        // Nothing in the domain or FluentValidation layer rejects a price with more than 2
        // decimal places — only ProductConfiguration's `HasPrecision(18, 2)` constrains it, and
        // that column facet is only enforced by a real relational engine, never by InMemory.
        var product = Product.Create(100_001, "Precision Test", null, 19.995m, 1);

        await using (var writeContext = CreateContext())
        {
            writeContext.Database.IsRelational().Should().BeTrue(
                "this test only proves anything against the real decimal(18,2) column, not InMemory's unconstrained storage");

            var repository = new ProductRepository(writeContext);
            await repository.AddAsync(product);
        }

        await using var readContext = CreateContext();
        var stored = await new ProductRepository(readContext).GetByIdAsync(100_001);

        stored.Should().NotBeNull();
        stored!.Price.Should().Be(20.00m,
            "SQL Server rounds (does not truncate or reject) a value that doesn't fit decimal(18,2) on write, " +
            "silently changing the exact value the caller submitted");
    }

    [SkippableFact]
    public async Task SearchByNameAsync_WithALiteralPercentWildcardInTheQuery_ShouldNotMatchEverythingOnRealSqlServer()
    {
        SkipIfNoRealSqlServer();

        // ProductRepository builds `%{name}%` without escaping SQL LIKE's own wildcard characters
        // (`%`, `_`, `[`). A product literally named "100% Cotton" turns the search term "100%
        // Cotton" into the pattern "%100% Cotton%" — still fine here — but a bare "%" search term
        // becomes the pattern "%%%", which matches every row instead of only names containing a
        // literal "%". InMemory's LIKE emulation may not reproduce this at all, so this only
        // proves anything against the real SQL Server LIKE parser.
        await using (var writeContext = CreateContext())
        {
            var repository = new ProductRepository(writeContext);
            await repository.AddAsync(Product.Create(100_001, "Zeiss Lens Cleaner", null, 1m, 0));
            await repository.AddAsync(Product.Create(100_002, "Microfiber Cloth", null, 1m, 0));
        }

        await using var readContext = CreateContext();
        var result = await new ProductRepository(readContext).SearchByNameAsync("%");

        result.Should().BeEmpty(
            "a search for a literal '%' should match products whose name actually contains '%' (none here), " +
            "not silently degrade into a wildcard that returns every product — this documents a real, " +
            "unescaped-LIKE bug in SearchByNameAsync that only a real SQL Server LIKE parser exposes");
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
