using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProductManager.WebAPI.Integration.Tests;

public class AuthRateLimitingTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthRateLimitingTests()
    {
        _factory = new ProductManagerWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("RateLimiting:Enabled", "true");
                builder.UseSetting("RateLimiting:PermitLimit", "2");
                builder.UseSetting("RateLimiting:WindowSeconds", "60");
            });
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Login_WhenAuthLimitExceeded_ShouldReturn429OnlyOnAuthEndpoints()
    {
        var loginBody = new { email = "ratelimit@example.com", password = "Password123!" };

        var first = await _client.PostAsJsonAsync("/api/auth/login", loginBody);
        var second = await _client.PostAsJsonAsync("/api/auth/login", loginBody);
        var third = await _client.PostAsJsonAsync("/api/auth/login", loginBody);

        first.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        second.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);

        third.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        third.Headers.RetryAfter.Should().NotBeNull();

        var body = await third.Content.ReadFromJsonAsync<RateLimitResponseBody>();
        body.Should().NotBeNull();
        body!.Message.Should().Be("Too many requests. Try again later.");

        var products = await _client.GetAsync("/api/products");
        products.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record RateLimitResponseBody(string Message);
}
