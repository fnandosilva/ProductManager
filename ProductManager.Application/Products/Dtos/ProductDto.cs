using ProductManger.Domain.Entities;

namespace ProductManager.Application.Products.Dtos;

public sealed record ProductDto(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock)
{
    public static ProductDto FromEntity(Product product) =>
        new(product.Id, product.Name, product.Description, product.Price, product.Stock);
}
