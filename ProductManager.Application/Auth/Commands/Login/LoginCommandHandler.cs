using FluentValidation;
using MediatR;
using ProductManager.Application.Auth.Dtos;
using ProductManager.Domain.Entities;
using ProductManager.Domain.Repositories;
using ProductManager.Domain.Services;

namespace ProductManager.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IAuthRepository _authRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginCommandHandler(
        IAuthRepository authRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _authRepository = authRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _authRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new ValidationException("Invalid email or password.");
        }

        var accessToken = _jwtTokenGenerator.GenerateToken(user);
        var generated = _refreshTokenGenerator.Generate();
        await _refreshTokenRepository.AddAsync(
            RefreshToken.Create(user, generated.TokenHash, generated.ExpiresAt),
            cancellationToken);

        return new AuthResponse(accessToken, user.Username, user.Email, generated.Token);
    }
}
