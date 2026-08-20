using FluentValidation;
using MediatR;
using ProductManager.Application.Products.Dtos;
using ProductManager.Domain.Repositories;

namespace ProductManager.Application.Products.Queries.GetProductsByStockLevel;

public sealed record GetProductsByStockLevelQuery(int Min, int Max) : IRequest<IReadOnlyList<ProductDto>>;

public sealed class GetProductsByStockLevelQueryValidator : AbstractValidator<GetProductsByStockLevelQuery>
{
    public GetProductsByStockLevelQueryValidator()
    {
        RuleFor(x => x.Min)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stock cannot be negative.");

        RuleFor(x => x.Max)
            .GreaterThanOrEqualTo(x => x.Min)
            .WithMessage("Maximum stock must be greater than or equal to minimum stock.");
    }
}

public sealed class GetProductsByStockLevelQueryHandler
    : IRequestHandler<GetProductsByStockLevelQuery, IReadOnlyList<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsByStockLevelQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyList<ProductDto>> Handle(
        GetProductsByStockLevelQuery request,
        CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetByStockRangeAsync(
            request.Min,
            request.Max,
            cancellationToken);

        return products.Select(ProductDto.FromEntity).ToList();
    }
}
