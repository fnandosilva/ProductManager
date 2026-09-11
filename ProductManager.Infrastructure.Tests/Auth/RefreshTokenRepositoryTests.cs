using FluentAssertions;
using ProductManager.Domain.Entities;
using ProductManager.Infrastructure.Auth;

namespace ProductManager.Infrastructure.Tests.Auth;

public class RefreshTokenRepositoryTests
{
    [Fact]
    public async Task AddAsync_ShouldPersistTokenAndLoadUserOnLookup()
    {
        using var context = TestDbContextFactory.Create();
        var authRepository = new AuthRepository(context);
        var repository = new RefreshTokenRepository(context);
        var user = User.Create("johndoe", "john@example.com", "hashed-password");
        await authRepository.AddAsync(user);

        var token = RefreshToken.Create(user, "token-hash-value", DateTime.UtcNow.AddDays(7));
        await repository.AddAsync(token);

        var stored = await repository.GetByTokenHashAsync("token-hash-value");
        stored.Should().NotBeNull();
        stored!.User.Should().NotBeNull();
        stored.User.Email.Should().Be("john@example.com");
        stored.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RotateAsync_ShouldRevokeOldTokenAndInsertReplacementInOneCommit()
    {
        using var context = TestDbContextFactory.Create();
        var authRepository = new AuthRepository(context);
        var repository = new RefreshTokenRepository(context);
        var user = User.Create("johndoe", "john@example.com", "hashed-password");
        await authRepository.AddAsync(user);

        var existing = RefreshToken.Create(user, "old-hash", DateTime.UtcNow.AddDays(7));
        await repository.AddAsync(existing);

        existing.Revoke("new-hash");
        var replacement = RefreshToken.Create(user, "new-hash", DateTime.UtcNow.AddDays(7));
        await repository.RotateAsync(existing, replacement);

        var oldToken = await repository.GetByTokenHashAsync("old-hash");
        var newToken = await repository.GetByTokenHashAsync("new-hash");

        oldToken.Should().NotBeNull();
        oldToken!.IsRevoked.Should().BeTrue();
        oldToken.ReplacedByTokenHash.Should().Be("new-hash");
        newToken.Should().NotBeNull();
        newToken!.IsActive.Should().BeTrue();
        context.RefreshTokens.Count().Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistRevocation()
    {
        using var context = TestDbContextFactory.Create();
        var authRepository = new AuthRepository(context);
        var repository = new RefreshTokenRepository(context);
        var user = User.Create("johndoe", "john@example.com", "hashed-password");
        await authRepository.AddAsync(user);

        var token = RefreshToken.Create(user, "hash", DateTime.UtcNow.AddDays(7));
        await repository.AddAsync(token);

        token.Revoke();
        await repository.UpdateAsync(token);

        var stored = await repository.GetByTokenHashAsync("hash");
        stored!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task GetByTokenHashAsync_WithUnknownHash_ShouldReturnNull()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new RefreshTokenRepository(context);

        var stored = await repository.GetByTokenHashAsync("missing");

        stored.Should().BeNull();
    }
}
