namespace ProductManager.Application.Auth.Dtos;

public sealed record AuthResponse(
    string Token,
    string Username,
    string Email);
