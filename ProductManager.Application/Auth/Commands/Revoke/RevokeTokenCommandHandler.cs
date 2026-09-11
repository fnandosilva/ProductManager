using MediatR;
using ProductManager.Domain.Repositories;
using ProductManager.Domain.Services;

namespace ProductManager.Application.Auth.Commands.Revoke;

public sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;

    public RevokeTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenGenerator refreshTokenGenerator)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenGenerator = refreshTokenGenerator;
    }

    public async Task Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var existing = await _refreshTokenRepository.GetByTokenHashAsync(
            _refreshTokenGenerator.Hash(request.RefreshToken),
            cancellationToken);

        if (existing is null || existing.IsRevoked)
        {
            return;
        }

        existing.Revoke();
        await _refreshTokenRepository.UpdateAsync(existing, cancellationToken);
    }
}
