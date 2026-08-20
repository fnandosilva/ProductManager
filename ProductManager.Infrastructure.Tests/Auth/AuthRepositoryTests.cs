using FluentAssertions;
using ProductManager.Infrastructure.Auth;
using ProductManager.Domain.Entities;

namespace ProductManager.Infrastructure.Tests.Auth;

public class AuthRepositoryTests
{
    [Fact]
    public async Task AddAsync_ShouldPersistUser()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new AuthRepository(context);
        var user = User.Create("johndoe", "john@example.com", "hashed-password");

        await repository.AddAsync(user);

        var stored = await repository.GetByEmailAsync("john@example.com");
        stored.Should().NotBeNull();
        stored!.Username.Should().Be("johndoe");
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldBeCaseInsensitive()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new AuthRepository(context);
        await repository.AddAsync(User.Create("johndoe", "john@example.com", "hashed-password"));

        var stored = await repository.GetByEmailAsync("JOHN@EXAMPLE.COM");

        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistentEmail_ShouldReturnNull()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new AuthRepository(context);

        var stored = await repository.GetByEmailAsync("missing@example.com");

        stored.Should().BeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_ShouldReturnMatchingUser()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new AuthRepository(context);
        await repository.AddAsync(User.Create("johndoe", "john@example.com", "hashed-password"));

        var stored = await repository.GetByUsernameAsync("johndoe");

        stored.Should().NotBeNull();
        stored!.Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task EmailExistsAsync_WithExistingEmail_ShouldReturnTrue()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new AuthRepository(context);
        await repository.AddAsync(User.Create("johndoe", "john@example.com", "hashed-password"));

        var exists = await repository.EmailExistsAsync("john@example.com");

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_WithNonExistentEmail_ShouldReturnFalse()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new AuthRepository(context);

        var exists = await repository.EmailExistsAsync("missing@example.com");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task UsernameExistsAsync_WithExistingUsername_ShouldReturnTrue()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new AuthRepository(context);
        await repository.AddAsync(User.Create("johndoe", "john@example.com", "hashed-password"));

        var exists = await repository.UsernameExistsAsync("johndoe");

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task UsernameExistsAsync_WithNonExistentUsername_ShouldReturnFalse()
    {
        using var context = TestDbContextFactory.Create();
        var repository = new AuthRepository(context);

        var exists = await repository.UsernameExistsAsync("missing");

        exists.Should().BeFalse();
    }
}
