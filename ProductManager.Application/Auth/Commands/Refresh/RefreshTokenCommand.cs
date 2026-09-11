using FluentValidation;
using MediatR;
using ProductManager.Application.Auth.Dtos;

namespace ProductManager.Application.Auth.Commands.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
