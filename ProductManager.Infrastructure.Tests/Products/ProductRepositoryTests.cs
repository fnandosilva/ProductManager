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
    public async Task SearchByNameAsync_WithALiteralPercentInTheQuery_ShouldOnlyMatchNamesContainingIt()
    {
        // Regression test for the unescaped-LIKE bug documented in
        // RealSqlServer/ProductRepositoryRealSqlServerTests.cs: a search term of "%" must not
        // be reinterpreted as a wildcard that matches every product.
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);
        await repository.AddAsync(Product.Create(100_001, "100% Cotton Cloth", null, 1m, 0));
        await repository.AddAsync(Product.Create(100_002, "Zeiss Lens Cleaner", null, 1m, 0));

        var result = await repository.SearchByNameAsync("%");

        result.Should().ContainSingle(p => p.Id == 100_001);
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
    public async Task AddAsync_WithMoreThanTwoDecimalPlaces_ShouldPreserveFullPrecisionOnInMemory()
    {
        // Contrast with RealSqlServer/ProductRepositoryRealSqlServerTests.cs: InMemory never
        // applies the decimal(18,2) column facet from ProductConfiguration, so this value
        // round-trips exactly here — unlike a real SQL Server, which silently rounds it.
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);
        await repository.AddAsync(Product.Create(100_001, "Precision Test", null, 19.995m, 1));

        var stored = await repository.GetByIdAsync(100_001);

        stored!.Price.Should().Be(19.995m);
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

    [Fact]
    public async Task DecrementStockAsync_WithSufficientStock_ShouldPersistTheDecrement()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);
        await repository.AddAsync(Product.Create(100_001, "Test", null, 10m, 10));

        var result = await repository.DecrementStockAsync(100_001, 4);

        result.Should().NotBeNull();
        result!.Stock.Should().Be(6);
        (await repository.GetByIdAsync(100_001))!.Stock.Should().Be(6);
    }

    [Fact]
    public async Task DecrementStockAsync_WithInsufficientStock_ShouldThrowAndNotPersistAnyChange()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);
        await repository.AddAsync(Product.Create(100_001, "Test", null, 10m, 2));

        var act = () => repository.DecrementStockAsync(100_001, 10);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Insufficient stock*");
        (await repository.GetByIdAsync(100_001))!.Stock.Should().Be(2);
    }

    [Fact]
    public async Task DecrementStockAsync_WithNonExistentProduct_ShouldReturnNull()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);

        var result = await repository.DecrementStockAsync(999_999, 1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddToStockAsync_WithExistingProduct_ShouldPersistTheIncrement()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);
        await repository.AddAsync(Product.Create(100_001, "Test", null, 10m, 5));

        var result = await repository.AddToStockAsync(100_001, 20);

        result.Should().NotBeNull();
        result!.Stock.Should().Be(25);
        (await repository.GetByIdAsync(100_001))!.Stock.Should().Be(25);
    }

    [Fact]
    public async Task AddToStockAsync_WithNonExistentProduct_ShouldReturnNull()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new ProductRepository(context);

        var result = await repository.AddToStockAsync(999_999, 1);

        result.Should().BeNull();
    }
}
