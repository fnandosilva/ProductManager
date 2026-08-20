using FluentAssertions;
using Moq;
using ProductManager.Application.Common.Exceptions;
using ProductManager.Application.Products.Commands.AddToStock;
using ProductManager.Application.Products.Commands.DecrementStock;
using ProductManager.Application.Products.Commands.DeleteProduct;
using ProductManager.Domain.Entities;
using ProductManager.Domain.Repositories;

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
    public async Task Handle_WithExistingProduct_ShouldCallAddToStockAsyncWithTheRequestedQuantity()
    {
        var product = Product.Create(100_001, "Test", null, 10m, 15);
        _repository
            .Setup(r => r.AddToStockAsync(100_001, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var handler = new AddToStockCommandHandler(_repository.Object);
        await handler.Handle(new AddToStockCommand(100_001, 10), CancellationToken.None);

        _repository.Verify(r => r.AddToStockAsync(100_001, 10, It.IsAny<CancellationToken>()), Times.Once);
        // The actual +/- arithmetic and locking now live in the repository (see
        // IProductRepository.AddToStockAsync) precisely so a real relational engine can make the
        // read-modify-write atomic — a handler-level mock can't exercise that, only that the
        // handler delegates to it. See RealSqlServer/ProductRepositoryStockConcurrencyRealSqlServerTests
        // for the real-concurrency proof.
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldThrowNotFoundException()
    {
        _repository
            .Setup(r => r.AddToStockAsync(999_999, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var handler = new AddToStockCommandHandler(_repository.Object);
        var act = () => handler.Handle(new AddToStockCommand(999_999, 10), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class DecrementStockCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();

    [Fact]
    public async Task Handle_WithSufficientStock_ShouldCallDecrementStockAsyncWithTheRequestedQuantity()
    {
        var product = Product.Create(100_001, "Test", null, 10m, 2);
        _repository
            .Setup(r => r.DecrementStockAsync(100_001, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var handler = new DecrementStockCommandHandler(_repository.Object);
        await handler.Handle(new DecrementStockCommand(100_001, 3), CancellationToken.None);

        _repository.Verify(r => r.DecrementStockAsync(100_001, 3, It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WithInsufficientStock_ShouldPropagateInvalidOperationException()
    {
        // Product.DecrementStock's insufficient-stock check now runs inside
        // IProductRepository.DecrementStockAsync's locked read-modify-write (see
        // ProductRepositoryTests for real coverage of that behavior against InMemory, and
        // RealSqlServer/ProductRepositoryStockConcurrencyRealSqlServerTests against a real
        // engine) — the handler's only job is to let the exception propagate unchanged.
        _repository
            .Setup(r => r.DecrementStockAsync(100_001, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Insufficient stock. Available: 2, requested: 10."));

        var handler = new DecrementStockCommandHandler(_repository.Object);
        var act = () => handler.Handle(new DecrementStockCommand(100_001, 10), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Insufficient stock*");
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldThrowNotFoundException()
    {
        _repository
            .Setup(r => r.DecrementStockAsync(999_999, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var handler = new DecrementStockCommandHandler(_repository.Object);
        var act = () => handler.Handle(new DecrementStockCommand(999_999, 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
