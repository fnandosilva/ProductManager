using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ProductManager.Infrastructure.Security;
using ProductManger.Domain.Entities;

namespace ProductManager.Infrastructure.Tests.Security;

public class JwtTokenGeneratorTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = "TestSecretKeyThatIsAtLeast32CharactersLong!",
            ["JwtSettings:Issuer"] = "TestIssuer",
            ["JwtSettings:Audience"] = "TestAudience",
            ["JwtSettings:ExpiryMinutes"] = "60"
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
    public void GenerateToken_ShouldReturnNonEmptyToken()
    {
        var generator = new JwtTokenGenerator(BuildConfiguration());
        var user = User.Create("johndoe", "john@example.com", "hashed-password");

        var token = generator.GenerateToken(user);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateToken_ShouldEmbedExpectedClaims()
    {
        var generator = new JwtTokenGenerator(BuildConfiguration());
        var user = User.Create("johndoe", "john@example.com", "hashed-password");

        var token = generator.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Issuer.Should().Be("TestIssuer");
        jwt.Audiences.Should().Contain("TestAudience");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "john@example.com");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == "johndoe");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateToken_ShouldSetExpiryBasedOnConfiguration()
    {
        var generator = new JwtTokenGenerator(BuildConfiguration(new Dictionary<string, string?>
        {
            ["JwtSettings:ExpiryMinutes"] = "30"
        }));
        var user = User.Create("johndoe", "john@example.com", "hashed-password");

        var before = DateTime.UtcNow;
        var token = generator.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.ValidTo.Should().BeCloseTo(before.AddMinutes(30), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateToken_CalledTwice_ShouldProduceDifferentJtiClaims()
    {
        var generator = new JwtTokenGenerator(BuildConfiguration());
        var user = User.Create("johndoe", "john@example.com", "hashed-password");
        var handler = new JwtSecurityTokenHandler();

        var jwt1 = handler.ReadJwtToken(generator.GenerateToken(user));
        var jwt2 = handler.ReadJwtToken(generator.GenerateToken(user));

        var jti1 = jwt1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = jwt2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        jti1.Should().NotBe(jti2);
    }

    [Fact]
    public void GenerateToken_WithoutSecretKey_ShouldThrowInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "TestIssuer"
        }).Build();

        var generator = new JwtTokenGenerator(configuration);
        var user = User.Create("johndoe", "john@example.com", "hashed-password");

        var act = () => generator.GenerateToken(user);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GenerateToken_WithoutIssuerAndAudience_ShouldUseDefaults()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = "TestSecretKeyThatIsAtLeast32CharactersLong!"
        }).Build();

        var generator = new JwtTokenGenerator(configuration);
        var user = User.Create("johndoe", "john@example.com", "hashed-password");

        var token = generator.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Issuer.Should().Be("ProductManagerAPI");
        jwt.Audiences.Should().Contain("ProductManagerClient");
    }
}
