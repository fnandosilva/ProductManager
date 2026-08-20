using ProductManager.Domain.Entities;

namespace ProductManager.Domain.Repositories;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> SearchByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetByStockRangeAsync(int min, int max, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(Product product, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically decrements stock: the read-check-write is serialized against a real relational
    /// engine (row lock + transaction) so two concurrent requests against the same product can
    /// never both read the same "before" stock and silently lose one of the two decrements — the
    /// classic race an inventory API must not have. Returns <c>null</c> if the product doesn't
    /// exist. Still throws <see cref="InvalidOperationException"/> for insufficient stock, via
    /// <see cref="Product.DecrementStock"/> — that business rule doesn't change, only when it's
    /// safe to evaluate it.
    /// </summary>
    Task<Product?> DecrementStockAsync(int id, int quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically adds to stock — see <see cref="DecrementStockAsync"/> for why this needs the
    /// same locked read-modify-write instead of a plain get-then-update.
    /// </summary>
    Task<Product?> AddToStockAsync(int id, int quantity, CancellationToken cancellationToken = default);
}
