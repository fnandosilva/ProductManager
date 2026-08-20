using MediatR;
using ProductManager.Application.Common.Exceptions;
using ProductManager.Application.Products.Dtos;
using ProductManager.Domain.Repositories;

namespace ProductManager.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(int Id) : IRequest<ProductDto>;

public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IProductRepository _productRepository;

    public GetProductByIdQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Product with ID {request.Id} was not found.");

        return ProductDto.FromEntity(product);
    }
}
