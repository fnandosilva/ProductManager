using MediatR;
using ProductManager.Application.Products.Dtos;
using ProductManager.Domain.Entities;
using ProductManager.Domain.Repositories;
using ProductManager.Domain.Services;

namespace ProductManager.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductIdGenerator _productIdGenerator;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        IProductIdGenerator productIdGenerator)
    {
        _productRepository = productRepository;
        _productIdGenerator = productIdGenerator;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var id = await _productIdGenerator.GenerateNextIdAsync(cancellationToken);

        var product = Product.Create(
            id,
            request.Name,
            request.Description,
            request.Price,
            request.Stock);

        await _productRepository.AddAsync(product, cancellationToken);

        return ProductDto.FromEntity(product);
    }
}
