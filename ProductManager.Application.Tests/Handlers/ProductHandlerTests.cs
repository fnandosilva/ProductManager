using FluentAssertions;
using Moq;
using ProductManager.Application.Common.Exceptions;
using ProductManager.Application.Products.Commands.CreateProduct;
using ProductManager.Application.Products.Commands.UpdateProduct;
using ProductManager.Domain.Entities;
using ProductManager.Domain.Repositories;
using ProductManager.Domain.Services;

namespace ProductManager.Application.Tests.Handlers;

public class ProductHandlerTests
{
    [Fact]
    public async Task CreateProductHandler_ShouldGenerateIdAndPersistProduct()
    {
        var repository = new Mock<IProductRepository>();
        var idGenerator = new Mock<IProductIdGenerator>();
        idGenerator.Setup(g => g.GenerateNextIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(100_010);

        Product? savedProduct = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((product, _) => savedProduct = product)
            .Returns(Task.CompletedTask);

        var handler = new CreateProductCommandHandler(repository.Object, idGenerator.Object);
        var result = await handler.Handle(
            new CreateProductCommand("New Product", "Desc", 15.50m, 20),
            CancellationToken.None);

        result.Id.Should().Be(100_010);
        result.Name.Should().Be("New Product");
        result.Stock.Should().Be(20);
        savedProduct.Should().NotBeNull();
        savedProduct!.Id.Should().Be(100_010);
    }

    [Fact]
    public async Task UpdateProductHandler_WhenProductNotFound_ShouldThrowNotFoundException()
    {
        var repository = new Mock<IProductRepository>();
        repository
            .Setup(r => r.GetByIdAsync(100_001, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var handler = new UpdateProductCommandHandler(repository.Object);

        var act = () => handler.Handle(
            new UpdateProductCommand(100_001, "Name", null, 10m, 5),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
