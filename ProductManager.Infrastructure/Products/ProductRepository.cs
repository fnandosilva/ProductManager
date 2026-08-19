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
        return await _context.Products
            .AsNoTracking()
            .Where(p => EF.Functions.Like(p.Name, $"%{name}%"))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
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
}
