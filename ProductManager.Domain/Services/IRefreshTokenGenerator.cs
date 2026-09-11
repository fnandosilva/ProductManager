namespace ProductManager.Domain.Services;

public sealed record GeneratedRefreshToken(
    string Token,
    string TokenHash,
    DateTime ExpiresAt);

public interface IRefreshTokenGenerator
{
    GeneratedRefreshToken Generate();
    string Hash(string token);
}
