namespace ProductManager.Domain.Entities;

public class Product
{
    public const int MinId = 100_000;
    public const int MaxId = 999_999;

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }

    private Product() { }

    public static Product Create(int id, string name, string? description, decimal price, int stock)
    {
        ValidateId(id);
        ValidateName(name);
        ValidatePrice(price);
        ValidateStock(stock);

        return new Product
        {
            Id = id,
            Name = name.Trim(),
            Description = description?.Trim(),
            Price = price,
            Stock = stock
        };
    }

    public void Update(string name, string? description, decimal price, int stock)
    {
        ValidateName(name);
        ValidatePrice(price);
        ValidateStock(stock);

        Name = name.Trim();
        Description = description?.Trim();
        Price = price;
        Stock = stock;
    }

    public void DecrementStock(int quantity)
    {
        ValidateQuantity(quantity);

        if (Stock < quantity)
        {
            throw new InvalidOperationException(
                $"Insufficient stock. Available: {Stock}, requested: {quantity}.");
        }

        Stock -= quantity;
    }

    public void AddToStock(int quantity)
    {
        ValidateQuantity(quantity);
        Stock += quantity;
    }

    private static void ValidateId(int id)
    {
        if (id is < MinId or > MaxId)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Product ID must be a 6-digit number.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (name.Trim().Length > 200)
        {
            throw new ArgumentException("Product name cannot exceed 200 characters.", nameof(name));
        }
    }

    private static void ValidatePrice(decimal price)
    {
        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        }
    }

    private static void ValidateStock(int stock)
    {
        if (stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stock), "Stock cannot be negative.");
        }
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }
    }
}
