using FluentAssertions;
using ProductManager.Infrastructure.Security;

namespace ProductManager.Infrastructure.Tests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ShouldNotReturnPlainText()
    {
        var hash = _hasher.HashPassword("password123");

        hash.Should().NotBe("password123");
        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HashPassword_CalledTwiceWithSamePassword_ShouldProduceDifferentHashes()
    {
        var hash1 = _hasher.HashPassword("password123");
        var hash2 = _hasher.HashPassword("password123");

        hash1.Should().NotBe(hash2, "BCrypt uses a random salt per hash");
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        var hash = _hasher.HashPassword("password123");

        var result = _hasher.VerifyPassword("password123", hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        var hash = _hasher.HashPassword("password123");

        var result = _hasher.VerifyPassword("wrongpassword", hash);

        result.Should().BeFalse();
    }
}
