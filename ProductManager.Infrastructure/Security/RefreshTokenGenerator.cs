using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using ProductManager.Domain.Services;

namespace ProductManager.Infrastructure.Security;

public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private readonly IConfiguration _configuration;

    public RefreshTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public GeneratedRefreshToken Generate()
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var expiryDays = int.Parse(jwtSettings["RefreshTokenExpiryDays"] ?? "7");

        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Base64Url.EncodeToString(bytes);
        var expiresAt = DateTime.UtcNow.AddDays(expiryDays);

        return new GeneratedRefreshToken(token, Hash(token), expiresAt);
    }

    public string Hash(string token)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(hashBytes);
    }
}
