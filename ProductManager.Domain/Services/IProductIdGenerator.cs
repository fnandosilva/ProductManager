namespace ProductManger.Domain.Services;

public interface IProductIdGenerator
{
    Task<int> GenerateNextIdAsync(CancellationToken cancellationToken = default);
}
