using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProductManager.Infrastructure.Data.Seed;
using ProductManger.Domain.Entities;

namespace ProductManager.Infrastructure.Tests.Data;

public class DatabaseSeederTests
{
    [Fact]
    public async Task SeedRequiredDataAsync_OnEmptyDatabase_ShouldSeedProductIdSequence()
    {
        using var context = TestDbContextFactory.Create();

        await DatabaseSeeder.SeedRequiredDataAsync(context);

        var sequence = await context.ProductIdSequences.SingleAsync();
        sequence.NextProductId.Should().Be(Product.MinId);
    }

    [Fact]
    public async Task SeedRequiredDataAsync_CalledTwice_ShouldNotDuplicateTheSequenceRow()
    {
        using var context = TestDbContextFactory.Create();

        await DatabaseSeeder.SeedRequiredDataAsync(context);
        await DatabaseSeeder.SeedRequiredDataAsync(context);

        var sequences = await context.ProductIdSequences.ToListAsync();
        sequences.Should().ContainSingle();
    }

    [Fact]
    public async Task SeedSampleDataAsync_OnEmptyDatabase_ShouldSeedProductsAndBumpTheSequence()
    {
        using var context = TestDbContextFactory.Create();
        await DatabaseSeeder.SeedRequiredDataAsync(context);

        await DatabaseSeeder.SeedSampleDataAsync(context);

        var sequence = await context.ProductIdSequences.SingleAsync();
        sequence.NextProductId.Should().Be(100_006);

        var products = await context.Products.ToListAsync();
        products.Should().HaveCount(5);
        products.Should().OnlyContain(p => p.Id >= 100_001 && p.Id <= 100_005);
    }

    [Fact]
    public async Task SeedSampleDataAsync_CalledTwice_ShouldNotDuplicateProducts()
    {
        using var context = TestDbContextFactory.Create();
        await DatabaseSeeder.SeedRequiredDataAsync(context);

        await DatabaseSeeder.SeedSampleDataAsync(context);
        await DatabaseSeeder.SeedSampleDataAsync(context);

        var products = await context.Products.ToListAsync();
        products.Should().HaveCount(5);
    }

    [Fact]
    public async Task SeedSampleDataAsync_WhenProductsAlreadyExist_ShouldNotReseed()
    {
        using var context = TestDbContextFactory.Create();
        await context.Database.EnsureCreatedAsync();
        context.Products.Add(Product.Create(100_999, "Existing Product", null, 1m, 1));
        context.ProductIdSequences.Add(new ProductIdSequence { Id = 1, NextProductId = 101_000 });
        await context.SaveChangesAsync();

        await DatabaseSeeder.SeedSampleDataAsync(context);

        var products = await context.Products.ToListAsync();
        products.Should().ContainSingle();
        products[0].Id.Should().Be(100_999);
    }

    [Fact]
    public async Task SeedSampleDataAsync_ShouldSeedProductsWithPositiveStock()
    {
        using var context = TestDbContextFactory.Create();
        await DatabaseSeeder.SeedRequiredDataAsync(context);

        await DatabaseSeeder.SeedSampleDataAsync(context);

        var products = await context.Products.ToListAsync();
        products.Should().OnlyContain(p => p.Stock > 0);
    }

    [Fact]
    public async Task SeedSampleDataAsync_OnEmptyDatabase_ShouldSeedOneDemoUserWithWorkingPassword()
    {
        using var context = TestDbContextFactory.Create();
        await DatabaseSeeder.SeedRequiredDataAsync(context);

        await DatabaseSeeder.SeedSampleDataAsync(context);

        var users = await context.Users.ToListAsync();
        users.Should().ContainSingle();
        users[0].Username.Should().Be("demo");
        users[0].Email.Should().Be("demo@productmanager.local");
        BCrypt.Net.BCrypt.Verify("Demo@1234", users[0].PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task SeedSampleDataAsync_CalledTwice_ShouldNotDuplicateDemoUser()
    {
        using var context = TestDbContextFactory.Create();
        await DatabaseSeeder.SeedRequiredDataAsync(context);

        await DatabaseSeeder.SeedSampleDataAsync(context);
        await DatabaseSeeder.SeedSampleDataAsync(context);

        var users = await context.Users.ToListAsync();
        users.Should().ContainSingle();
    }

    [Fact]
    public async Task SeedSampleDataAsync_WhenProductsAlreadyExistButNoUsers_ShouldStillSeedDemoUser()
    {
        using var context = TestDbContextFactory.Create();
        await context.Database.EnsureCreatedAsync();
        context.Products.Add(Product.Create(100_999, "Existing Product", null, 1m, 1));
        context.ProductIdSequences.Add(new ProductIdSequence { Id = 1, NextProductId = 101_000 });
        await context.SaveChangesAsync();

        await DatabaseSeeder.SeedSampleDataAsync(context);

        var users = await context.Users.ToListAsync();
        users.Should().ContainSingle();
        users[0].Username.Should().Be("demo");
    }
}
