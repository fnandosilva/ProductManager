using ProductManger.Domain.Entities;

namespace ProductManger.Domain.Repositories;

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
}
