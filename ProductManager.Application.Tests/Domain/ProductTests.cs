using FluentAssertions;
using ProductManger.Domain.Entities;

namespace ProductManager.Application.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateProduct()
    {
        var product = Product.Create(100_001, "Test Product", "Description", 9.99m, 10);

        product.Id.Should().Be(100_001);
        product.Name.Should().Be("Test Product");
        product.Description.Should().Be("Description");
        product.Price.Should().Be(9.99m);
        product.Stock.Should().Be(10);
    }

    [Theory]
    [InlineData(99999)]
    [InlineData(1_000_000)]
    public void Create_WithInvalidId_ShouldThrow(int invalidId)
    {
        var act = () => Product.Create(invalidId, "Test", null, 1m, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DecrementStock_WithInsufficientStock_ShouldThrow()
    {
        var product = Product.Create(100_001, "Test", null, 1m, 5);

        var act = () => product.DecrementStock(10);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient stock*");
    }

    [Fact]
    public void AddToStock_WithValidQuantity_ShouldIncreaseStock()
    {
        var product = Product.Create(100_001, "Test", null, 1m, 5);

        product.AddToStock(3);

        product.Stock.Should().Be(8);
    }

    [Fact]
    public void DecrementStock_WithExactStock_ShouldReduceToZero()
    {
        var product = Product.Create(100_001, "Test", null, 1m, 5);

        product.DecrementStock(5);

        product.Stock.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DecrementStock_WithNonPositiveQuantity_ShouldThrow(int quantity)
    {
        var product = Product.Create(100_001, "Test", null, 1m, 5);

        var act = () => product.DecrementStock(quantity);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddToStock_WithNonPositiveQuantity_ShouldThrow(int quantity)
    {
        var product = Product.Create(100_001, "Test", null, 1m, 5);

        var act = () => product.AddToStock(quantity);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => Product.Create(100_001, "   ", null, 1m, 0);

        act.Should().Throw<ArgumentException>().WithMessage("*Product name is required*");
    }

    [Fact]
    public void Create_WithNameExceeding200Characters_ShouldThrow()
    {
        var longName = new string('a', 201);

        var act = () => Product.Create(100_001, longName, null, 1m, 0);

        act.Should().Throw<ArgumentException>().WithMessage("*cannot exceed 200 characters*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10.5)]
    public void Create_WithNonPositivePrice_ShouldThrow(double price)
    {
        var act = () => Product.Create(100_001, "Test", null, (decimal)price, 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNegativeStock_ShouldThrow()
    {
        var act = () => Product.Create(100_001, "Test", null, 1m, -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_ShouldTrimNameAndDescription()
    {
        var product = Product.Create(100_001, "  Test Product  ", "  Description  ", 1m, 0);

        product.Name.Should().Be("Test Product");
        product.Description.Should().Be("Description");
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateAllFields()
    {
        var product = Product.Create(100_001, "Original", "Original Desc", 1m, 5);

        product.Update("Updated", "Updated Desc", 20m, 15);

        product.Name.Should().Be("Updated");
        product.Description.Should().Be("Updated Desc");
        product.Price.Should().Be(20m);
        product.Stock.Should().Be(15);
        product.Id.Should().Be(100_001);
    }

    [Fact]
    public void Update_WithEmptyName_ShouldThrow()
    {
        var product = Product.Create(100_001, "Original", null, 1m, 5);

        var act = () => product.Update("", null, 1m, 5);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithNegativePrice_ShouldThrow()
    {
        var product = Product.Create(100_001, "Original", null, 1m, 5);

        var act = () => product.Update("Original", null, -1m, 5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
