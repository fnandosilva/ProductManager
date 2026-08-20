using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProductManager.Infrastructure.Data.Seed;
using ProductManger.Domain.Entities;

namespace ProductManager.Infrastructure.Tests.Data;

public class DatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_OnEmptyDatabase_ShouldSeedProductIdSequenceAndProducts()
    {
        using var context = TestDbContextFactory.Create();

        await DatabaseSeeder.SeedAsync(context);

        var sequence = await context.ProductIdSequences.SingleAsync();
        sequence.NextProductId.Should().Be(100_006);

        var products = await context.Products.ToListAsync();
        products.Should().HaveCount(5);
        products.Should().OnlyContain(p => p.Id >= 100_001 && p.Id <= 100_005);
    }

    [Fact]
    public async Task SeedAsync_CalledTwice_ShouldNotDuplicateProducts()
    {
        using var context = TestDbContextFactory.Create();

        await DatabaseSeeder.SeedAsync(context);
        await DatabaseSeeder.SeedAsync(context);

        var products = await context.Products.ToListAsync();
        products.Should().HaveCount(5);
    }

    [Fact]
    public async Task SeedAsync_WhenProductsAlreadyExist_ShouldNotReseed()
    {
        using var context = TestDbContextFactory.Create();
        await context.Database.EnsureCreatedAsync();
        context.Products.Add(Product.Create(100_999, "Existing Product", null, 1m, 1));
        context.ProductIdSequences.Add(new ProductIdSequence { Id = 1, NextProductId = 101_000 });
        await context.SaveChangesAsync();

        await DatabaseSeeder.SeedAsync(context);

        var products = await context.Products.ToListAsync();
        products.Should().ContainSingle();
        products[0].Id.Should().Be(100_999);
    }

    [Fact]
    public async Task SeedAsync_ShouldSeedProductsWithPositiveStock()
    {
        using var context = TestDbContextFactory.Create();

        await DatabaseSeeder.SeedAsync(context);

        var products = await context.Products.ToListAsync();
        products.Should().OnlyContain(p => p.Stock > 0);
    }

    [Fact]
    public async Task SeedAsync_OnEmptyDatabase_ShouldSeedOneDemoUserWithWorkingPassword()
    {
        using var context = TestDbContextFactory.Create();

        await DatabaseSeeder.SeedAsync(context);

        var users = await context.Users.ToListAsync();
        users.Should().ContainSingle();
        users[0].Username.Should().Be("demo");
        users[0].Email.Should().Be("demo@productmanager.local");
        BCrypt.Net.BCrypt.Verify("Demo@1234", users[0].PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_CalledTwice_ShouldNotDuplicateDemoUser()
    {
        using var context = TestDbContextFactory.Create();

        await DatabaseSeeder.SeedAsync(context);
        await DatabaseSeeder.SeedAsync(context);

        var users = await context.Users.ToListAsync();
        users.Should().ContainSingle();
    }

    [Fact]
    public async Task SeedAsync_WhenProductsAlreadyExistButNoUsers_ShouldStillSeedDemoUser()
    {
        using var context = TestDbContextFactory.Create();
        await context.Database.EnsureCreatedAsync();
        context.Products.Add(Product.Create(100_999, "Existing Product", null, 1m, 1));
        context.ProductIdSequences.Add(new ProductIdSequence { Id = 1, NextProductId = 101_000 });
        await context.SaveChangesAsync();

        await DatabaseSeeder.SeedAsync(context);

        var users = await context.Users.ToListAsync();
        users.Should().ContainSingle();
        users[0].Username.Should().Be("demo");
    }
}
