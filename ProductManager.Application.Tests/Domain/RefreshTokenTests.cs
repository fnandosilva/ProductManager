using FluentAssertions;
using ProductManager.Domain.Entities;

namespace ProductManager.Application.Tests.Domain;

public class RefreshTokenTests
{
    private static User CreateUser() => User.Create("johndoe", "john@example.com", "hashed-password");

    [Fact]
    public void Create_WithValidData_ShouldCreateActiveToken()
    {
        var user = CreateUser();
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var token = RefreshToken.Create(user, "abc123hash", expiresAt);

        token.User.Should().BeSameAs(user);
        token.UserId.Should().Be(user.Id);
        token.TokenHash.Should().Be("abc123hash");
        token.ExpiresAt.Should().Be(expiresAt);
        token.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        token.RevokedAt.Should().BeNull();
        token.ReplacedByTokenHash.Should().BeNull();
        token.IsRevoked.Should().BeFalse();
        token.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_WithEmptyHash_ShouldThrow(string? hash)
    {
        var act = () => RefreshToken.Create(CreateUser(), hash!, DateTime.UtcNow.AddDays(7));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullUser_ShouldThrow()
    {
        var act = () => RefreshToken.Create(null!, "abc123hash", DateTime.UtcNow.AddDays(7));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Revoke_ShouldMarkTokenInactiveAndStoreReplacementHash()
    {
        var token = RefreshToken.Create(CreateUser(), "abc123hash", DateTime.UtcNow.AddDays(7));

        token.Revoke("replacement-hash");

        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();
        token.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        token.ReplacedByTokenHash.Should().Be("replacement-hash");
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsInThePast_ShouldBeTrue()
    {
        var token = RefreshToken.Create(CreateUser(), "abc123hash", DateTime.UtcNow.AddMinutes(-1));

        token.IsExpired.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }
}
