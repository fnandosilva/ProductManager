using MediatR;
using ProductManager.Application.Common.Exceptions;
using ProductManager.Domain.Repositories;

namespace ProductManager.Application.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(int Id) : IRequest;

public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IProductRepository _productRepository;

    public DeleteProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Product with ID {request.Id} was not found.");

        await _productRepository.DeleteAsync(product, cancellationToken);
    }
}
