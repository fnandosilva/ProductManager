using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProductManager.Application.Products.Commands.AddToStock;
using ProductManager.Application.Products.Commands.CreateProduct;
using ProductManager.Application.Products.Commands.DecrementStock;
using ProductManager.Application.Products.Commands.DeleteProduct;
using ProductManager.Application.Products.Commands.UpdateProduct;
using ProductManager.Application.Products.Dtos;
using ProductManager.Application.Products.Queries.GetProductById;
using ProductManager.Application.Products.Queries.GetProducts;
using ProductManager.Application.Products.Queries.GetProductsByStockLevel;
using ProductManager.Application.Products.Queries.SearchProducts;
using ProductManager.Presentation.Products;

namespace ProductManager.Presentation.Tests.Products;

public class ProductsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _controller = new ProductsController(_sender.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithProducts()
    {
        var products = new List<ProductDto> { new(100_001, "Test", null, 10m, 5) };
        _sender.Setup(s => s.Send(It.IsAny<GetProductsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(products);
    }

    [Fact]
    public async Task Search_ShouldSendSearchQueryWithGivenName()
    {
        var products = new List<ProductDto> { new(100_001, "Zeiss Lens", null, 10m, 5) };
        _sender.Setup(s => s.Send(It.Is<SearchProductsQuery>(q => q.Name == "Zeiss"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var result = await _controller.Search("Zeiss", CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(products);
        _sender.Verify(s => s.Send(It.Is<SearchProductsQuery>(q => q.Name == "Zeiss"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByStockLevel_ShouldSendQueryWithMinAndMax()
    {
        var products = new List<ProductDto>();
        _sender.Setup(s => s.Send(It.IsAny<GetProductsByStockLevelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var result = await _controller.GetByStockLevel(0, 10, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _sender.Verify(
            s => s.Send(It.Is<GetProductsByStockLevelQuery>(q => q.Min == 0 && q.Max == 10), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetById_ShouldReturnOkWithProduct()
    {
        var product = new ProductDto(100_001, "Test", null, 10m, 5);
        _sender.Setup(s => s.Send(It.Is<GetProductByIdQuery>(q => q.Id == 100_001), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _controller.GetById(100_001, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(product);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtActionWithProduct()
    {
        var product = new ProductDto(100_001, "New Product", "Desc", 15m, 20);
        _sender.Setup(s => s.Send(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var request = new CreateProductRequest("New Product", "Desc", 15m, 20);
        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(ProductsController.GetById));
        createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(100_001);
        createdResult.Value.Should().BeSameAs(product);

        _sender.Verify(
            s => s.Send(
                It.Is<CreateProductCommand>(c => c.Name == "New Product" && c.Price == 15m && c.Stock == 20),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Update_ShouldReturnOkWithUpdatedProduct()
    {
        var product = new ProductDto(100_001, "Updated", "Desc", 20m, 10);
        _sender.Setup(s => s.Send(It.IsAny<UpdateProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var request = new UpdateProductRequest("Updated", "Desc", 20m, 10);
        var result = await _controller.Update(100_001, request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(product);
        _sender.Verify(
            s => s.Send(It.Is<UpdateProductCommand>(c => c.Id == 100_001 && c.Name == "Updated"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DecrementStock_ShouldSendCommandAndReturnOk()
    {
        _sender.Setup(s => s.Send(It.IsAny<DecrementStockCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.DecrementStock(100_001, 5, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _sender.Verify(
            s => s.Send(It.Is<DecrementStockCommand>(c => c.Id == 100_001 && c.Quantity == 5), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddToStock_ShouldSendCommandAndReturnOk()
    {
        _sender.Setup(s => s.Send(It.IsAny<AddToStockCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.AddToStock(100_001, 5, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _sender.Verify(
            s => s.Send(It.Is<AddToStockCommand>(c => c.Id == 100_001 && c.Quantity == 5), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldSendCommandAndReturnNoContent()
    {
        _sender.Setup(s => s.Send(It.IsAny<DeleteProductCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.Delete(100_001, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _sender.Verify(
            s => s.Send(It.Is<DeleteProductCommand>(c => c.Id == 100_001), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
