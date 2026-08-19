using FluentAssertions;
using ProductManager.Application.Products.Commands.AddToStock;
using ProductManager.Application.Products.Commands.CreateProduct;
using ProductManager.Application.Products.Commands.DecrementStock;
using ProductManager.Application.Products.Commands.UpdateProduct;
using ProductManager.Application.Products.Queries.GetProductsByStockLevel;
using ProductManager.Application.Products.Queries.SearchProducts;

namespace ProductManager.Application.Tests.Validators;

public class ProductValidatorTests
{
    [Fact]
    public void CreateProductValidator_WithEmptyName_ShouldHaveError()
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(string.Empty, null, 10m, 5);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Name));
    }

    [Fact]
    public void CreateProductValidator_WithNameExceedingMaxLength_ShouldHaveError()
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(new string('a', 201), null, 10m, 5);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Name));
    }

    [Fact]
    public void CreateProductValidator_WithDescriptionExceedingMaxLength_ShouldHaveError()
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand("Valid Name", new string('a', 1001), 10m, 5);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Description));
    }

    [Fact]
    public void CreateProductValidator_WithNullDescription_ShouldNotHaveDescriptionError()
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand("Valid Name", null, 10m, 5);

        var result = validator.Validate(command);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateProductCommand.Description));
    }

    [Fact]
    public void CreateProductValidator_WithInvalidPrice_ShouldHaveError()
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand("Valid Name", null, 0m, 5);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Price));
    }

    [Fact]
    public void CreateProductValidator_WithNegativeStock_ShouldHaveError()
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand("Valid Name", null, 10m, -1);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Stock));
    }

    [Fact]
    public void CreateProductValidator_WithValidData_ShouldNotHaveErrors()
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand("Valid Name", "Valid description", 10m, 5);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateProductValidator_WithNegativeStock_ShouldHaveError()
    {
        var validator = new UpdateProductCommandValidator();
        var command = new UpdateProductCommand(100_001, "Valid Name", null, 10m, -1);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductCommand.Stock));
    }

    [Theory]
    [InlineData(99_999)]
    [InlineData(1_000_000)]
    public void UpdateProductValidator_WithInvalidId_ShouldHaveError(int invalidId)
    {
        var validator = new UpdateProductCommandValidator();
        var command = new UpdateProductCommand(invalidId, "Valid Name", null, 10m, 5);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductCommand.Id));
    }

    [Fact]
    public void UpdateProductValidator_WithValidData_ShouldNotHaveErrors()
    {
        var validator = new UpdateProductCommandValidator();
        var command = new UpdateProductCommand(100_001, "Valid Name", null, 10m, 5);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}

public class AddToStockCommandValidatorTests
{
    private readonly AddToStockCommandValidator _validator = new();

    [Theory]
    [InlineData(99_999)]
    [InlineData(1_000_000)]
    public void Validate_WithInvalidId_ShouldHaveError(int invalidId)
    {
        var result = _validator.Validate(new AddToStockCommand(invalidId, 5));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddToStockCommand.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_WithNonPositiveQuantity_ShouldHaveError(int quantity)
    {
        var result = _validator.Validate(new AddToStockCommand(100_001, quantity));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddToStockCommand.Quantity));
    }

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveErrors()
    {
        var result = _validator.Validate(new AddToStockCommand(100_001, 5));

        result.IsValid.Should().BeTrue();
    }
}

public class DecrementStockCommandValidatorTests
{
    private readonly DecrementStockCommandValidator _validator = new();

    [Theory]
    [InlineData(99_999)]
    [InlineData(1_000_000)]
    public void Validate_WithInvalidId_ShouldHaveError(int invalidId)
    {
        var result = _validator.Validate(new DecrementStockCommand(invalidId, 5));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DecrementStockCommand.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_WithNonPositiveQuantity_ShouldHaveError(int quantity)
    {
        var result = _validator.Validate(new DecrementStockCommand(100_001, quantity));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DecrementStockCommand.Quantity));
    }

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveErrors()
    {
        var result = _validator.Validate(new DecrementStockCommand(100_001, 5));

        result.IsValid.Should().BeTrue();
    }
}

public class SearchProductsQueryValidatorTests
{
    private readonly SearchProductsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithEmptyName_ShouldHaveError()
    {
        var result = _validator.Validate(new SearchProductsQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SearchProductsQuery.Name));
    }

    [Fact]
    public void Validate_WithValidName_ShouldNotHaveErrors()
    {
        var result = _validator.Validate(new SearchProductsQuery("Zeiss"));

        result.IsValid.Should().BeTrue();
    }
}

public class GetProductsByStockLevelQueryValidatorTests
{
    private readonly GetProductsByStockLevelQueryValidator _validator = new();

    [Fact]
    public void Validate_WithNegativeMin_ShouldHaveError()
    {
        var result = _validator.Validate(new GetProductsByStockLevelQuery(-1, 10));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetProductsByStockLevelQuery.Min));
    }

    [Fact]
    public void Validate_WithMaxLessThanMin_ShouldHaveError()
    {
        var result = _validator.Validate(new GetProductsByStockLevelQuery(10, 5));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetProductsByStockLevelQuery.Max));
    }

    [Fact]
    public void Validate_WithEqualMinAndMax_ShouldNotHaveErrors()
    {
        var result = _validator.Validate(new GetProductsByStockLevelQuery(5, 5));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithValidRange_ShouldNotHaveErrors()
    {
        var result = _validator.Validate(new GetProductsByStockLevelQuery(0, 100));

        result.IsValid.Should().BeTrue();
    }
}
