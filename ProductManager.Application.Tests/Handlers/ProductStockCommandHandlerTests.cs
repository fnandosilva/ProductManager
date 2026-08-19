using FluentAssertions;
using Moq;
using ProductManager.Application.Common.Exceptions;
using ProductManager.Application.Products.Commands.AddToStock;
using ProductManager.Application.Products.Commands.DecrementStock;
using ProductManager.Application.Products.Commands.DeleteProduct;
using ProductManger.Domain.Entities;
using ProductManger.Domain.Repositories;

namespace ProductManager.Application.Tests.Handlers;

public class DeleteProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();

    [Fact]
    public async Task Handle_WithExistingProduct_ShouldDeleteIt()
    {
        var product = Product.Create(100_001, "Test", null, 10m, 5);
        _repository.Setup(r => r.GetByIdAsync(100_001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = new DeleteProductCommandHandler(_repository.Object);
        await handler.Handle(new DeleteProductCommand(100_001), CancellationToken.None);

        _repository.Verify(r => r.DeleteAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldThrowNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(999_999, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var handler = new DeleteProductCommandHandler(_repository.Object);
        var act = () => handler.Handle(new DeleteProductCommand(999_999), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class AddToStockCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();

    [Fact]
    public async Task Handle_WithExistingProduct_ShouldIncreaseStockAndPersist()
    {
        var product = Product.Create(100_001, "Test", null, 10m, 5);
        _repository.Setup(r => r.GetByIdAsync(100_001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = new AddToStockCommandHandler(_repository.Object);
        await handler.Handle(new AddToStockCommand(100_001, 10), CancellationToken.None);

        product.Stock.Should().Be(15);
        _repository.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldThrowNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(999_999, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var handler = new AddToStockCommandHandler(_repository.Object);
        var act = () => handler.Handle(new AddToStockCommand(999_999, 10), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class DecrementStockCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();

    [Fact]
    public async Task Handle_WithSufficientStock_ShouldDecreaseStockAndPersist()
    {
        var product = Product.Create(100_001, "Test", null, 10m, 5);
        _repository.Setup(r => r.GetByIdAsync(100_001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = new DecrementStockCommandHandler(_repository.Object);
        await handler.Handle(new DecrementStockCommand(100_001, 3), CancellationToken.None);

        product.Stock.Should().Be(2);
        _repository.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInsufficientStock_ShouldThrowInvalidOperationException()
    {
        var product = Product.Create(100_001, "Test", null, 10m, 2);
        _repository.Setup(r => r.GetByIdAsync(100_001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = new DecrementStockCommandHandler(_repository.Object);
        var act = () => handler.Handle(new DecrementStockCommand(100_001, 10), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Insufficient stock*");
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldThrowNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(999_999, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var handler = new DecrementStockCommandHandler(_repository.Object);
        var act = () => handler.Handle(new DecrementStockCommand(999_999, 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
