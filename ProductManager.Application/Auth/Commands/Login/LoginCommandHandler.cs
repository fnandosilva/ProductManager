using FluentValidation;
using MediatR;
using ProductManager.Application.Auth.Dtos;
using ProductManger.Domain.Repositories;
using ProductManger.Domain.Services;

namespace ProductManager.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IAuthRepository _authRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IAuthRepository authRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _authRepository = authRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _authRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new ValidationException("Invalid email or password.");
        }

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResponse(token, user.Username, user.Email);
    }
}
