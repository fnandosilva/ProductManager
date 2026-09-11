using FluentValidation;
using MediatR;

namespace ProductManager.Application.Auth.Commands.Revoke;

public sealed record RevokeTokenCommand(string RefreshToken) : IRequest;

public sealed class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
