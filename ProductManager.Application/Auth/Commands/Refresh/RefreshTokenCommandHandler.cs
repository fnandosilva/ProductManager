using MediatR;
using ProductManager.Application.Auth.Dtos;
using ProductManager.Application.Common.Exceptions;
using ProductManager.Domain.Entities;
using ProductManager.Domain.Repositories;
using ProductManager.Domain.Services;

namespace ProductManager.Application.Auth.Commands.Refresh;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenGenerator refreshTokenGenerator,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenGenerator = refreshTokenGenerator;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existing = await _refreshTokenRepository.GetByTokenHashAsync(
            _refreshTokenGenerator.Hash(request.RefreshToken),
            cancellationToken);

        if (existing is null || !existing.IsActive)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        var accessToken = _jwtTokenGenerator.GenerateToken(existing.User);
        var generated = _refreshTokenGenerator.Generate();
        var replacement = RefreshToken.Create(existing.User, generated.TokenHash, generated.ExpiresAt);

        existing.Revoke(generated.TokenHash);
        await _refreshTokenRepository.RotateAsync(existing, replacement, cancellationToken);

        return new AuthResponse(accessToken, existing.User.Username, existing.User.Email, generated.Token);
    }
}
