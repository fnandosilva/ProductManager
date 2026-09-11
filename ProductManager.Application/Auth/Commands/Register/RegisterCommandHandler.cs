using FluentValidation;
using MediatR;
using ProductManager.Application.Auth.Dtos;
using ProductManager.Domain.Entities;
using ProductManager.Domain.Repositories;
using ProductManager.Domain.Services;

namespace ProductManager.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IAuthRepository _authRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public RegisterCommandHandler(
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

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _authRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new ValidationException("Email is already registered.");
        }

        if (await _authRepository.UsernameExistsAsync(request.Username, cancellationToken))
        {
            throw new ValidationException("Username is already taken.");
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = User.Create(request.Username, request.Email, passwordHash);

        await _authRepository.AddAsync(user, cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateToken(user);
        var generated = _refreshTokenGenerator.Generate();
        await _refreshTokenRepository.AddAsync(
            RefreshToken.Create(user, generated.TokenHash, generated.ExpiresAt),
            cancellationToken);

        return new AuthResponse(accessToken, user.Username, user.Email, generated.Token);
    }
}
