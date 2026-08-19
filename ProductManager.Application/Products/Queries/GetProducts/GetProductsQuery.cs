using MediatR;
using ProductManager.Application.Products.Dtos;
using ProductManger.Domain.Repositories;

namespace ProductManager.Application.Products.Queries.GetProducts;

public sealed record GetProductsQuery : IRequest<IReadOnlyList<ProductDto>>;

public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        return products.Select(ProductDto.FromEntity).ToList();
    }
}
