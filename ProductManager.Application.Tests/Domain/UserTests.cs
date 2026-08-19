using FluentAssertions;
using ProductManger.Domain.Entities;

namespace ProductManager.Application.Tests.Domain;

public class UserTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateUser()
    {
        var user = User.Create("johndoe", "john@example.com", "hashed-password");

        user.Username.Should().Be("johndoe");
        user.Email.Should().Be("john@example.com");
        user.PasswordHash.Should().Be("hashed-password");
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldNormalizeEmailToLowercase()
    {
        var user = User.Create("johndoe", "JOHN@EXAMPLE.COM", "hashed-password");

        user.Email.Should().Be("john@example.com");
    }

    [Fact]
    public void Create_ShouldTrimUsername()
    {
        var user = User.Create("  johndoe  ", "john@example.com", "hashed-password");

        user.Username.Should().Be("johndoe");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    public void Create_WithInvalidUsername_ShouldThrow(string invalidUsername)
    {
        var act = () => User.Create(invalidUsername, "john@example.com", "hashed-password");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithTooLongUsername_ShouldThrow()
    {
        var longUsername = new string('a', 51);

        var act = () => User.Create(longUsername, "john@example.com", "hashed-password");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("not-an-email")]
    public void Create_WithInvalidEmail_ShouldThrow(string invalidEmail)
    {
        var act = () => User.Create("johndoe", invalidEmail, "hashed-password");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_WithEmptyPasswordHash_ShouldThrow(string? invalidHash)
    {
        var act = () => User.Create("johndoe", "john@example.com", invalidHash!);

        act.Should().Throw<ArgumentException>();
    }
}
