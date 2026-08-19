using System.Data;
using Microsoft.EntityFrameworkCore;
using ProductManager.Infrastructure.Data;
using ProductManger.Domain.Entities;
using ProductManger.Domain.Services;

namespace ProductManager.Infrastructure.Security;

public class ProductIdGenerator : IProductIdGenerator
{
    private readonly AppDbContext _context;

    public ProductIdGenerator(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> GenerateNextIdAsync(CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsRelational())
        {
            return await GenerateForNonRelationalDatabaseAsync(cancellationToken);
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var productId = await AllocateNextIdAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return productId;
        });
    }

    private async Task<int> GenerateForNonRelationalDatabaseAsync(CancellationToken cancellationToken)
    {
        return await AllocateNextIdAsync(cancellationToken);
    }

    private async Task<int> AllocateNextIdAsync(CancellationToken cancellationToken)
    {
        ProductIdSequence sequence;

        if (_context.Database.IsSqlServer())
        {
            sequence = await _context.ProductIdSequences
                .FromSqlRaw("SELECT * FROM ProductIdSequences WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", 1)
                .AsTracking()
                .SingleAsync(cancellationToken);
        }
        else
        {
            sequence = await _context.ProductIdSequences
                .AsTracking()
                .SingleAsync(s => s.Id == 1, cancellationToken);
        }

        var productId = sequence.NextProductId;

        if (productId > Product.MaxId)
        {
            throw new InvalidOperationException("Product ID range exhausted.");
        }

        sequence.NextProductId = productId + 1;

        await _context.SaveChangesAsync(cancellationToken);

        return productId;
    }
}
