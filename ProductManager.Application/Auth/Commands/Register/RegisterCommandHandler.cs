using FluentValidation;
using MediatR;
using ProductManager.Application.Auth.Dtos;
using ProductManger.Domain.Entities;
using ProductManger.Domain.Repositories;
using ProductManger.Domain.Services;

namespace ProductManager.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IAuthRepository _authRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterCommandHandler(
        IAuthRepository authRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _authRepository = authRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
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

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResponse(token, user.Username, user.Email);
    }
}
