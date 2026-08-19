using FluentValidation;
using MediatR;
using ProductManager.Application.Auth.Dtos;

namespace ProductManager.Application.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<AuthResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
