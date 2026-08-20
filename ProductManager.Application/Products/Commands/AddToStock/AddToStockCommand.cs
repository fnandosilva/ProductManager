using FluentValidation;
using MediatR;
using ProductManager.Application.Common.Exceptions;
using ProductManger.Domain.Repositories;

namespace ProductManager.Application.Products.Commands.AddToStock;

public sealed record AddToStockCommand(int Id, int Quantity) : IRequest;

public sealed class AddToStockCommandValidator : AbstractValidator<AddToStockCommand>
{
    public AddToStockCommandValidator()
    {
        RuleFor(x => x.Id)
            .InclusiveBetween(100_000, 999_999)
            .WithMessage("Product ID must be a 6-digit number.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
    }
}

public sealed class AddToStockCommandHandler : IRequestHandler<AddToStockCommand>
{
    private readonly IProductRepository _productRepository;

    public AddToStockCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task Handle(AddToStockCommand request, CancellationToken cancellationToken)
    {
        // Atomic (locked) read-modify-write — see IProductRepository.AddToStockAsync — so two
        // concurrent stock additions against the same product can never both read the same
        // "before" stock and lose one of the two updates.
        _ = await _productRepository.AddToStockAsync(request.Id, request.Quantity, cancellationToken)
            ?? throw new NotFoundException($"Product with ID {request.Id} was not found.");
    }
}
