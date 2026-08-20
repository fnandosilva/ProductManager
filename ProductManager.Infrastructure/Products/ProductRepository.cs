using System.Data;
using Microsoft.EntityFrameworkCore;
using ProductManger.Domain.Entities;
using ProductManger.Domain.Repositories;

namespace ProductManager.Infrastructure.Products;

public class ProductRepository : IProductRepository
{
    private readonly Data.AppDbContext _context;

    public ProductRepository(Data.AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> SearchByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        // The caller's search term is untrusted input, not a LIKE pattern: without escaping,
        // literal '%'/'_'/'[' characters in a product name search (e.g. "100% Cotton") would be
        // reinterpreted as SQL wildcards — a bare "%" search would then match every row instead
        // of only names actually containing '%'. Only a real relational LIKE parser exercises
        // this; EF Core's InMemory provider never surfaced it (see ProductManager.Infrastructure.Tests
        // /RealSqlServer/ProductRepositoryRealSqlServerTests.cs).
        var escapedName = EscapeLikePattern(name);

        return await _context.Products
            .AsNoTracking()
            .Where(p => EF.Functions.Like(p.Name, $"%{escapedName}%", "\\"))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_")
            .Replace("[", "\\[");
    }

    public async Task<IReadOnlyList<Product>> GetByStockRangeAsync(
        int min,
        int max,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Stock >= min && p.Stock <= max)
            .OrderBy(p => p.Stock)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products.AnyAsync(p => p.Id == id, cancellationToken);
    }

    public Task<Product?> DecrementStockAsync(int id, int quantity, CancellationToken cancellationToken = default)
        => MutateUnderRowLockAsync(id, product => product.DecrementStock(quantity), cancellationToken);

    public Task<Product?> AddToStockAsync(int id, int quantity, CancellationToken cancellationToken = default)
        => MutateUnderRowLockAsync(id, product => product.AddToStock(quantity), cancellationToken);

    /// <summary>
    /// Reads a product, applies <paramref name="mutate"/>, and saves — all inside a single
    /// Serializable transaction, with the read itself taking a real row lock on a relational
    /// engine (same pattern as <c>ProductIdGenerator</c>'s counter allocation). Without this, two
    /// concurrent stock-adjustment requests can both read the same "before" value and one update
    /// silently overwrites the other (a lost update) — exactly the bug an inventory API can't
    /// afford. <paramref name="mutate"/> throwing (e.g. <see cref="Product.DecrementStock"/> on
    /// insufficient stock) rolls the transaction back instead of persisting a partial change.
    /// </summary>
    private async Task<Product?> MutateUnderRowLockAsync(
        int id,
        Action<Product> mutate,
        CancellationToken cancellationToken)
    {
        if (!_context.Database.IsRelational())
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (product is null)
            {
                return null;
            }

            mutate(product);
            await _context.SaveChangesAsync(cancellationToken);
            return product;
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            Product? product;
            if (_context.Database.IsSqlServer())
            {
                product = await _context.Products
                    .FromSqlRaw("SELECT * FROM Products WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", id)
                    .AsTracking()
                    .SingleOrDefaultAsync(cancellationToken);
            }
            else
            {
                product = await _context.Products
                    .AsTracking()
                    .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
            }

            if (product is null)
            {
                return null;
            }

            mutate(product);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return product;
        });
    }
}
