using FluentAssertions;
using ProductManager.Infrastructure.Products;
using ProductManger.Domain.Entities;

namespace ProductManager.Infrastructure.Tests.Products;

public class ProductRepositoryTests
{
    [Fact]
    public async Task AddAsync_ShouldPersistProduct()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);
        var product = Product.Create(100_001, "Test Product", "Desc", 10m, 5);

        await repository.AddAsync(product);

        var stored = await repository.GetByIdAsync(100_001);
        stored.Should().NotBeNull();
        stored!.Name.Should().Be("Test Product");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnProductsOrderedById()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);
        await repository.AddAsync(Product.Create(100_003, "C", null, 1m, 0));
        await repository.AddAsync(Product.Create(100_001, "A", null, 1m, 0));
        await repository.AddAsync(Product.Create(100_002, "B", null, 1m, 0));

        var result = await repository.GetAllAsync();

        result.Should().HaveCount(3);
        result.Select(p => p.Id).Should().ContainInOrder(100_001, 100_002, 100_003);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);

        var result = await repository.GetByIdAsync(999_999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SearchByNameAsync_ShouldReturnCaseInsensitivePartialMatches()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);
        await repository.AddAsync(Product.Create(100_001, "Zeiss Lens Cleaner", null, 1m, 0));
        await repository.AddAsync(Product.Create(100_002, "Microfiber Cloth", null, 1m, 0));

        var result = await repository.SearchByNameAsync("lens");

        result.Should().ContainSingle(p => p.Id == 100_001);
    }

    [Fact]
    public async Task SearchByNameAsync_WithNoMatches_ShouldReturnEmpty()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);
        await repository.AddAsync(Product.Create(100_001, "Zeiss Lens Cleaner", null, 1m, 0));

        var result = await repository.SearchByNameAsync("nonexistent");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByStockRangeAsync_ShouldReturnProductsWithinRange()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);
        await repository.AddAsync(Product.Create(100_001, "Low", null, 1m, 5));
        await repository.AddAsync(Product.Create(100_002, "Mid", null, 1m, 50));
        await repository.AddAsync(Product.Create(100_003, "High", null, 1m, 500));

        var result = await repository.GetByStockRangeAsync(10, 100);

        result.Should().ContainSingle(p => p.Id == 100_002);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);
        var product = Product.Create(100_001, "Original", null, 10m, 5);
        await repository.AddAsync(product);

        product.Update("Updated", "New Desc", 20m, 15);
        await repository.UpdateAsync(product);

        var stored = await repository.GetByIdAsync(100_001);
        stored!.Name.Should().Be("Updated");
        stored.Price.Should().Be(20m);
        stored.Stock.Should().Be(15);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveProduct()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);
        var product = Product.Create(100_001, "ToDelete", null, 10m, 5);
        await repository.AddAsync(product);

        await repository.DeleteAsync(product);

        var stored = await repository.GetByIdAsync(100_001);
        stored.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingProduct_ShouldReturnTrue()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);
        await repository.AddAsync(Product.Create(100_001, "Test", null, 10m, 5));

        var exists = await repository.ExistsAsync(100_001);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentProduct_ShouldReturnFalse()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);

        var exists = await repository.ExistsAsync(999_999);

        exists.Should().BeFalse();
    }
}
