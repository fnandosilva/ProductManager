namespace ProductManager.Domain.Entities;

public class User
{
    public int Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public static User Create(string username, string email, string passwordHash)
    {
        ValidateUsername(username);
        ValidateEmail(email);

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        return new User
        {
            Username = username.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static void ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        if (username.Trim().Length < 3)
        {
            throw new ArgumentException("Username must be at least 3 characters.", nameof(username));
        }

        if (username.Trim().Length > 50)
        {
            throw new ArgumentException("Username cannot exceed 50 characters.", nameof(username));
        }
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (!email.Contains('@'))
        {
            throw new ArgumentException("Invalid email format.", nameof(email));
        }
    }
}
