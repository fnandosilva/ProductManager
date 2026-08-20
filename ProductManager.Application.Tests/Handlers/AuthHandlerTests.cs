using FluentAssertions;
using FluentValidation;
using Moq;
using ProductManager.Application.Auth.Commands.Login;
using ProductManager.Application.Auth.Commands.Register;
using ProductManager.Domain.Entities;
using ProductManager.Domain.Repositories;
using ProductManager.Domain.Services;

namespace ProductManager.Application.Tests.Handlers;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IAuthRepository> _authRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();

    private RegisterCommandHandler CreateHandler() =>
        new(_authRepository.Object, _passwordHasher.Object, _jwtTokenGenerator.Object);

    [Fact]
    public async Task Handle_WithNewEmailAndUsername_ShouldCreateUserAndReturnToken()
    {
        _authRepository.Setup(r => r.EmailExistsAsync("new@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _authRepository.Setup(r => r.UsernameExistsAsync("newuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasher.Setup(h => h.HashPassword("password123")).Returns("hashed-password");
        _jwtTokenGenerator.Setup(g => g.GenerateToken(It.IsAny<User>())).Returns("fake-jwt-token");

        User? capturedUser = null;
        _authRepository
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => capturedUser = user)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RegisterCommand("newuser", "new@example.com", "password123"),
            CancellationToken.None);

        result.Token.Should().Be("fake-jwt-token");
        result.Username.Should().Be("newuser");
        result.Email.Should().Be("new@example.com");
        capturedUser.Should().NotBeNull();
        capturedUser!.PasswordHash.Should().Be("hashed-password");
        _authRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ShouldThrowValidationException()
    {
        _authRepository.Setup(r => r.EmailExistsAsync("taken@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        var act = () => handler.Handle(
            new RegisterCommand("newuser", "taken@example.com", "password123"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*already registered*");

        _authRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExistingUsername_ShouldThrowValidationException()
    {
        _authRepository.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _authRepository.Setup(r => r.UsernameExistsAsync("takenuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        var act = () => handler.Handle(
            new RegisterCommand("takenuser", "new@example.com", "password123"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*already taken*");

        _authRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class LoginCommandHandlerTests
{
    private readonly Mock<IAuthRepository> _authRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();

    private LoginCommandHandler CreateHandler() =>
        new(_authRepository.Object, _passwordHasher.Object, _jwtTokenGenerator.Object);

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnToken()
    {
        var user = User.Create("johndoe", "john@example.com", "hashed-password");

        _authRepository.Setup(r => r.GetByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyPassword("password123", "hashed-password")).Returns(true);
        _jwtTokenGenerator.Setup(g => g.GenerateToken(user)).Returns("fake-jwt-token");

        var handler = CreateHandler();
        var result = await handler.Handle(
            new LoginCommand("john@example.com", "password123"),
            CancellationToken.None);

        result.Token.Should().Be("fake-jwt-token");
        result.Username.Should().Be("johndoe");
        result.Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task Handle_WithNonExistentEmail_ShouldThrowValidationException()
    {
        _authRepository.Setup(r => r.GetByEmailAsync("missing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(
            new LoginCommand("missing@example.com", "password123"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ShouldThrowValidationException()
    {
        var user = User.Create("johndoe", "john@example.com", "hashed-password");

        _authRepository.Setup(r => r.GetByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyPassword("wrongpassword", "hashed-password")).Returns(false);

        var handler = CreateHandler();

        var act = () => handler.Handle(
            new LoginCommand("john@example.com", "wrongpassword"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid email or password*");

        _jwtTokenGenerator.Verify(g => g.GenerateToken(It.IsAny<User>()), Times.Never);
    }
}
