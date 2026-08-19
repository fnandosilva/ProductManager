using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace ProductManager.WebAPI.Integration.Tests;

public class AuthApiTests : IDisposable
{
    private readonly ProductManagerWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public AuthApiTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturn200WithTokenAndUserInfo()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "newuser",
            email = "newuser@example.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseBody>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.Username.Should().Be("newuser");
        body.Email.Should().Be("newuser@example.com");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturn400()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "firstuser",
            email = "duplicate@example.com",
            password = "Password123!"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "seconduser",
            email = "duplicate@example.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ShouldReturn400()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "sameusername",
            email = "first@example.com",
            password = "Password123!"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "sameusername",
            email = "second@example.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("", "valid@example.com", "Password123!")]
    [InlineData("ab", "valid@example.com", "Password123!")]
    [InlineData("validusername", "not-an-email", "Password123!")]
    [InlineData("validusername", "valid@example.com", "12345")]
    public async Task Register_WithInvalidData_ShouldReturn400(string username, string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { username, email, password });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200WithToken()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "loginuser",
            email = "loginuser@example.com",
            password = "Password123!"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "loginuser@example.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseBody>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.Username.Should().Be("loginuser");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn400()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "wrongpassuser",
            email = "wrongpass@example.com",
            password = "Password123!"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "wrongpass@example.com",
            password = "IncorrectPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ShouldReturn400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "doesnotexist@example.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("", "Password123!")]
    [InlineData("not-an-email", "Password123!")]
    [InlineData("valid@example.com", "")]
    public async Task Login_WithInvalidData_ShouldReturn400(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task IssuedToken_ShouldGrantAccessToProtectedProductsEndpoint()
    {
        var token = await _factory.RegisterAndGetTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record AuthResponseBody(string Token, string Username, string Email);
}
