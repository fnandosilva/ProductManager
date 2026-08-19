using FluentAssertions;
using ProductManager.Application.Auth.Commands.Login;
using ProductManager.Application.Auth.Commands.Register;

namespace ProductManager.Application.Tests.Validators;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveErrors()
    {
        var result = _validator.Validate(new RegisterCommand("johndoe", "john@example.com", "password123"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Validate_WithInvalidUsername_ShouldHaveError(string username)
    {
        var result = _validator.Validate(new RegisterCommand(username, "john@example.com", "password123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Username));
    }

    [Fact]
    public void Validate_WithTooLongUsername_ShouldHaveError()
    {
        var result = _validator.Validate(
            new RegisterCommand(new string('a', 51), "john@example.com", "password123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Username));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_ShouldHaveError(string email)
    {
        var result = _validator.Validate(new RegisterCommand("johndoe", email, "password123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    public void Validate_WithInvalidPassword_ShouldHaveError(string password)
    {
        var result = _validator.Validate(new RegisterCommand("johndoe", "john@example.com", password));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Password));
    }
}

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveErrors()
    {
        var result = _validator.Validate(new LoginCommand("john@example.com", "password123"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_ShouldHaveError(string email)
    {
        var result = _validator.Validate(new LoginCommand(email, "password123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Email));
    }

    [Fact]
    public void Validate_WithEmptyPassword_ShouldHaveError()
    {
        var result = _validator.Validate(new LoginCommand("john@example.com", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }
}
