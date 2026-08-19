namespace ProductManager.Presentation.Auth;

public sealed record RegisterRequest(
    string Username,
    string Email,
    string Password);

public sealed record LoginRequest(
    string Email,
    string Password);
