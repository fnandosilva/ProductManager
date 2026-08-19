using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProductManager.WebAPI.Integration.Tests;

public class ProductManagerWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = $"IntegrationTestsDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Database:UseInMemoryDatabase", "true");
        builder.UseSetting("Database:InMemoryDatabaseName", DatabaseName);

        // appsettings.json ships with an empty JwtSettings:SecretKey on purpose (see
        // Program.cs fail-fast check) so a real secret never ends up in source control.
        // Tests supply their own throwaway key instead of relying on any local/CI secret.
        builder.UseSetting("JwtSettings:SecretKey", "IntegrationTests_NotARealSecret_ThisIsOnly4TestingPurposes123!");
    }

    /// <summary>
    /// Registers a new user through the real /api/auth/register endpoint and returns the raw JWT token.
    /// </summary>
    public async Task<string> RegisterAndGetTokenAsync(
        HttpClient client,
        string? username = null,
        string? email = null,
        string password = "Password123!")
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        username ??= $"user_{suffix}";
        email ??= $"user_{suffix}@example.com";

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            email,
            password
        });

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResponseModel>();
        return result!.Token;
    }

    /// <summary>
    /// Creates an HttpClient that already carries a valid Authorization: Bearer header
    /// obtained by registering a brand-new user.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string? username = null, string? email = null)
    {
        var client = CreateClient();
        var token = await RegisterAndGetTokenAsync(client, username, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public sealed record AuthResponseModel(string Token, string Username, string Email);
}
