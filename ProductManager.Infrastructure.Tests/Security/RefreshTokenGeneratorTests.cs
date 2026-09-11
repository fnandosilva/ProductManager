using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ProductManager.Infrastructure.Security;

namespace ProductManager.Infrastructure.Tests.Security;

public class RefreshTokenGeneratorTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["JwtSettings:RefreshTokenExpiryDays"] = "7"
        };

        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                settings[key] = value;
            }
        }

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    [Fact]
    public void Generate_ShouldReturnNonEmptyTokenAndMatchingHash()
    {
        var generator = new RefreshTokenGenerator(BuildConfiguration());

        var generated = generator.Generate();

        generated.Token.Should().NotBeNullOrWhiteSpace();
        generated.TokenHash.Should().Be(generator.Hash(generated.Token));
        generated.TokenHash.Should().HaveLength(64);
    }

    [Fact]
    public void Generate_CalledTwice_ShouldProduceDifferentTokens()
    {
        var generator = new RefreshTokenGenerator(BuildConfiguration());

        var first = generator.Generate();
        var second = generator.Generate();

        first.Token.Should().NotBe(second.Token);
        first.TokenHash.Should().NotBe(second.TokenHash);
    }

    [Fact]
    public void Generate_ShouldSetExpiryFromConfiguration()
    {
        var generator = new RefreshTokenGenerator(BuildConfiguration(new Dictionary<string, string?>
        {
            ["JwtSettings:RefreshTokenExpiryDays"] = "3"
        }));

        var before = DateTime.UtcNow;
        var generated = generator.Generate();

        generated.ExpiresAt.Should().BeCloseTo(before.AddDays(3), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Hash_ShouldBeDeterministicLowercaseHex()
    {
        var generator = new RefreshTokenGenerator(BuildConfiguration());

        var hash1 = generator.Hash("same-token");
        var hash2 = generator.Hash("same-token");

        hash1.Should().Be(hash2);
        hash1.Should().MatchRegex("^[0-9a-f]{64}$");
    }
}
