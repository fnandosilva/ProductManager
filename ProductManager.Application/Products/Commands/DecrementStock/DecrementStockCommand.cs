using FluentValidation;
using MediatR;
using ProductManager.Application.Common.Exceptions;
using ProductManger.Domain.Repositories;

namespace ProductManager.Application.Products.Commands.DecrementStock;

public sealed record DecrementStockCommand(int Id, int Quantity) : IRequest;

public sealed class DecrementStockCommandValidator : AbstractValidator<DecrementStockCommand>
{
    public DecrementStockCommandValidator()
    {
        RuleFor(x => x.Id)
            .InclusiveBetween(100_000, 999_999)
            .WithMessage("Product ID must be a 6-digit number.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
    }
}

public sealed class DecrementStockCommandHandler : IRequestHandler<DecrementStockCommand>
{
    private readonly IProductRepository _productRepository;

    public DecrementStockCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task Handle(DecrementStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Product with ID {request.Id} was not found.");

        product.DecrementStock(request.Quantity);

        await _productRepository.UpdateAsync(product, cancellationToken);
    }
}
