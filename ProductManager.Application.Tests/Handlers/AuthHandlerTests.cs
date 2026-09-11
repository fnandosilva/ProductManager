using FluentAssertions;
using FluentValidation;
using Moq;
using ProductManager.Application.Auth.Commands.Login;
using ProductManager.Application.Auth.Commands.Refresh;
using ProductManager.Application.Auth.Commands.Register;
using ProductManager.Application.Auth.Commands.Revoke;
using ProductManager.Application.Common.Exceptions;
using ProductManager.Domain.Entities;
using ProductManager.Domain.Repositories;
using ProductManager.Domain.Services;

namespace ProductManager.Application.Tests.Handlers;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IAuthRepository> _authRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<IRefreshTokenGenerator> _refreshTokenGenerator = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();

    private RegisterCommandHandler CreateHandler() =>
        new(
            _authRepository.Object,
            _passwordHasher.Object,
            _jwtTokenGenerator.Object,
            _refreshTokenGenerator.Object,
            _refreshTokenRepository.Object);

    private void SetupSuccessfulTokenIssuance()
    {
        _jwtTokenGenerator.Setup(g => g.GenerateToken(It.IsAny<User>())).Returns("fake-jwt-token");
        _refreshTokenGenerator.Setup(g => g.Generate())
            .Returns(new GeneratedRefreshToken("refresh-plain", "refresh-hash", DateTime.UtcNow.AddDays(7)));
        _refreshTokenRepository
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_WithNewEmailAndUsername_ShouldCreateUserAndReturnTokens()
    {
        _authRepository.Setup(r => r.EmailExistsAsync("new@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _authRepository.Setup(r => r.UsernameExistsAsync("newuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasher.Setup(h => h.HashPassword("password123")).Returns("hashed-password");
        SetupSuccessfulTokenIssuance();

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
        result.RefreshToken.Should().Be("refresh-plain");
        result.Username.Should().Be("newuser");
        result.Email.Should().Be("new@example.com");
        capturedUser.Should().NotBeNull();
        capturedUser!.PasswordHash.Should().Be("hashed-password");
        _authRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
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
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
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
    private readonly Mock<IRefreshTokenGenerator> _refreshTokenGenerator = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();

    private LoginCommandHandler CreateHandler() =>
        new(
            _authRepository.Object,
            _passwordHasher.Object,
            _jwtTokenGenerator.Object,
            _refreshTokenGenerator.Object,
            _refreshTokenRepository.Object);

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnTokens()
    {
        var user = User.Create("johndoe", "john@example.com", "hashed-password");

        _authRepository.Setup(r => r.GetByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyPassword("password123", "hashed-password")).Returns(true);
        _jwtTokenGenerator.Setup(g => g.GenerateToken(user)).Returns("fake-jwt-token");
        _refreshTokenGenerator.Setup(g => g.Generate())
            .Returns(new GeneratedRefreshToken("refresh-plain", "refresh-hash", DateTime.UtcNow.AddDays(7)));
        _refreshTokenRepository
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new LoginCommand("john@example.com", "password123"),
            CancellationToken.None);

        result.Token.Should().Be("fake-jwt-token");
        result.RefreshToken.Should().Be("refresh-plain");
        result.Username.Should().Be("johndoe");
        result.Email.Should().Be("john@example.com");
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
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
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IRefreshTokenGenerator> _refreshTokenGenerator = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();

    private RefreshTokenCommandHandler CreateHandler() =>
        new(_refreshTokenRepository.Object, _refreshTokenGenerator.Object, _jwtTokenGenerator.Object);

    [Fact]
    public async Task Handle_WithActiveToken_ShouldRotateAndReturnNewPair()
    {
        var user = User.Create("johndoe", "john@example.com", "hashed-password");
        var existing = RefreshToken.Create(user, "old-hash", DateTime.UtcNow.AddDays(7));

        _refreshTokenGenerator.Setup(g => g.Hash("old-plain")).Returns("old-hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("old-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _jwtTokenGenerator.Setup(g => g.GenerateToken(user)).Returns("new-jwt");
        _refreshTokenGenerator.Setup(g => g.Generate())
            .Returns(new GeneratedRefreshToken("new-plain", "new-hash", DateTime.UtcNow.AddDays(7)));
        _refreshTokenRepository
            .Setup(r => r.RotateAsync(existing, It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var result = await handler.Handle(new RefreshTokenCommand("old-plain"), CancellationToken.None);

        result.Token.Should().Be("new-jwt");
        result.RefreshToken.Should().Be("new-plain");
        result.Username.Should().Be("johndoe");
        existing.IsRevoked.Should().BeTrue();
        existing.ReplacedByTokenHash.Should().Be("new-hash");
        _refreshTokenRepository.Verify(
            r => r.RotateAsync(existing, It.Is<RefreshToken>(t => t.TokenHash == "new-hash"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ShouldThrowUnauthorizedException()
    {
        _refreshTokenGenerator.Setup(g => g.Hash("missing")).Returns("missing-hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("missing-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new RefreshTokenCommand("missing"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Invalid refresh token*");
        _refreshTokenRepository.Verify(
            r => r.RotateAsync(It.IsAny<RefreshToken>(), It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ShouldThrowUnauthorizedException()
    {
        var user = User.Create("johndoe", "john@example.com", "hashed-password");
        var expired = RefreshToken.Create(user, "expired-hash", DateTime.UtcNow.AddMinutes(-1));

        _refreshTokenGenerator.Setup(g => g.Hash("expired-plain")).Returns("expired-hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("expired-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expired);

        var handler = CreateHandler();

        var act = () => handler.Handle(new RefreshTokenCommand("expired-plain"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WithRevokedToken_ShouldThrowUnauthorizedException()
    {
        var user = User.Create("johndoe", "john@example.com", "hashed-password");
        var revoked = RefreshToken.Create(user, "revoked-hash", DateTime.UtcNow.AddDays(7));
        revoked.Revoke();

        _refreshTokenGenerator.Setup(g => g.Hash("revoked-plain")).Returns("revoked-hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("revoked-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(revoked);

        var handler = CreateHandler();

        var act = () => handler.Handle(new RefreshTokenCommand("revoked-plain"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}

public class RevokeTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IRefreshTokenGenerator> _refreshTokenGenerator = new();

    private RevokeTokenCommandHandler CreateHandler() =>
        new(_refreshTokenRepository.Object, _refreshTokenGenerator.Object);

    [Fact]
    public async Task Handle_WithActiveToken_ShouldRevokeAndUpdate()
    {
        var user = User.Create("johndoe", "john@example.com", "hashed-password");
        var existing = RefreshToken.Create(user, "hash", DateTime.UtcNow.AddDays(7));

        _refreshTokenGenerator.Setup(g => g.Hash("plain")).Returns("hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _refreshTokenRepository.Setup(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        await handler.Handle(new RevokeTokenCommand("plain"), CancellationToken.None);

        existing.IsRevoked.Should().BeTrue();
        _refreshTokenRepository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ShouldBeIdempotent()
    {
        _refreshTokenGenerator.Setup(g => g.Hash("missing")).Returns("missing-hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("missing-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var handler = CreateHandler();
        await handler.Handle(new RevokeTokenCommand("missing"), CancellationToken.None);

        _refreshTokenRepository.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAlreadyRevokedToken_ShouldBeIdempotent()
    {
        var user = User.Create("johndoe", "john@example.com", "hashed-password");
        var existing = RefreshToken.Create(user, "hash", DateTime.UtcNow.AddDays(7));
        existing.Revoke();

        _refreshTokenGenerator.Setup(g => g.Hash("plain")).Returns("hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = CreateHandler();
        await handler.Handle(new RevokeTokenCommand("plain"), CancellationToken.None);

        _refreshTokenRepository.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
