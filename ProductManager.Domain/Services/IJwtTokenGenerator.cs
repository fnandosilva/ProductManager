using ProductManger.Domain.Entities;

namespace ProductManger.Domain.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
