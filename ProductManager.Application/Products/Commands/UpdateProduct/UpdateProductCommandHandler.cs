using MediatR;
using ProductManager.Application.Common.Exceptions;
using ProductManager.Application.Products.Dtos;
using ProductManager.Domain.Repositories;

namespace ProductManager.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Product with ID {request.Id} was not found.");

        product.Update(request.Name, request.Description, request.Price, request.Stock);

        await _productRepository.UpdateAsync(product, cancellationToken);

        return ProductDto.FromEntity(product);
    }
}
