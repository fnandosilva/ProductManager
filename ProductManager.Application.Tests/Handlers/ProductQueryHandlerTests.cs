using FluentAssertions;
using Moq;
using ProductManager.Application.Common.Exceptions;
using ProductManager.Application.Products.Queries.GetProductById;
using ProductManager.Application.Products.Queries.GetProducts;
using ProductManager.Application.Products.Queries.GetProductsByStockLevel;
using ProductManager.Application.Products.Queries.SearchProducts;
using ProductManger.Domain.Entities;
using ProductManger.Domain.Repositories;

namespace ProductManager.Application.Tests.Handlers;

public class GetProductsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnAllProductsAsDtos()
    {
        var repository = new Mock<IProductRepository>();
        var products = new[]
        {
            Product.Create(100_001, "Product A", null, 10m, 5),
            Product.Create(100_002, "Product B", null, 20m, 0)
        };
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var handler = new GetProductsQueryHandler(repository.Object);
        var result = await handler.Handle(new GetProductsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Id == 100_001 && p.Name == "Product A");
        result.Should().OnlyContain(p => p.Stock >= 0);
    }

    [Fact]
    public async Task Handle_WithNoProducts_ShouldReturnEmptyList()
    {
        var repository = new Mock<IProductRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Product>());

        var handler = new GetProductsQueryHandler(repository.Object);
        var result = await handler.Handle(new GetProductsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}

public class GetProductByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingProduct_ShouldReturnDto()
    {
        var repository = new Mock<IProductRepository>();
        var product = Product.Create(100_001, "Product A", "Desc", 10m, 5);
        repository.Setup(r => r.GetByIdAsync(100_001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = new GetProductByIdQueryHandler(repository.Object);
        var result = await handler.Handle(new GetProductByIdQuery(100_001), CancellationToken.None);

        result.Id.Should().Be(100_001);
        result.Name.Should().Be("Product A");
    }

    [Fact]
    public async Task Handle_WithNonExistentProduct_ShouldThrowNotFoundException()
    {
        var repository = new Mock<IProductRepository>();
        repository.Setup(r => r.GetByIdAsync(999_999, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var handler = new GetProductByIdQueryHandler(repository.Object);
        var act = () => handler.Handle(new GetProductByIdQuery(999_999), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class SearchProductsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnMatchingProducts()
    {
        var repository = new Mock<IProductRepository>();
        var products = new[] { Product.Create(100_001, "Zeiss Lens", null, 10m, 5) };
        repository.Setup(r => r.SearchByNameAsync("Zeiss", It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var handler = new SearchProductsQueryHandler(repository.Object);
        var result = await handler.Handle(new SearchProductsQuery("Zeiss"), CancellationToken.None);

        result.Should().ContainSingle(p => p.Name == "Zeiss Lens");
    }

    [Fact]
    public async Task Handle_WithNoMatches_ShouldReturnEmptyList()
    {
        var repository = new Mock<IProductRepository>();
        repository.Setup(r => r.SearchByNameAsync("Unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Product>());

        var handler = new SearchProductsQueryHandler(repository.Object);
        var result = await handler.Handle(new SearchProductsQuery("Unknown"), CancellationToken.None);

        result.Should().BeEmpty();
    }
}

public class GetProductsByStockLevelQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnProductsWithinRange()
    {
        var repository = new Mock<IProductRepository>();
        var products = new[] { Product.Create(100_001, "Low Stock Item", null, 10m, 3) };
        repository.Setup(r => r.GetByStockRangeAsync(0, 10, It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var handler = new GetProductsByStockLevelQueryHandler(repository.Object);
        var result = await handler.Handle(new GetProductsByStockLevelQuery(0, 10), CancellationToken.None);

        result.Should().ContainSingle(p => p.Stock == 3);
    }
}
