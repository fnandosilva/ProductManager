namespace ProductManager.Domain.Entities;

public class ProductIdSequence
{
    public int Id { get; set; } = 1;
    public int NextProductId { get; set; } = Product.MinId;
}
