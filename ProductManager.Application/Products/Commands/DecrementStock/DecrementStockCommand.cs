using FluentValidation;
using MediatR;
using ProductManager.Application.Common.Exceptions;
using ProductManager.Domain.Repositories;

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
        // Atomic (locked) read-modify-write — see IProductRepository.DecrementStockAsync — so two
        // concurrent decrements against the same product can never both read the same "before"
        // stock and lose one of the two updates.
        _ = await _productRepository.DecrementStockAsync(request.Id, request.Quantity, cancellationToken)
            ?? throw new NotFoundException($"Product with ID {request.Id} was not found.");
    }
}
