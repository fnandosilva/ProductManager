using FluentValidation;
using MediatR;
using ProductManager.Application.Products.Dtos;
using ProductManger.Domain.Repositories;

namespace ProductManager.Application.Products.Queries.SearchProducts;

public sealed record SearchProductsQuery(string Name) : IRequest<IReadOnlyList<ProductDto>>;

public sealed class SearchProductsQueryValidator : AbstractValidator<SearchProductsQuery>
{
    public SearchProductsQueryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Search name is required.");
    }
}

public sealed class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public SearchProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyList<ProductDto>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository.SearchByNameAsync(request.Name, cancellationToken);
        return products.Select(ProductDto.FromEntity).ToList();
    }
}
