using ProductManager.Domain.Entities;

namespace ProductManager.Domain.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
