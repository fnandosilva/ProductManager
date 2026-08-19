using FluentValidation;
using ProductManager.Application.Products.Dtos;

namespace ProductManager.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock) : MediatR.IRequest<ProductDto>;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .InclusiveBetween(100_000, 999_999)
            .WithMessage("Product ID must be a 6-digit number.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative.");
    }
}
