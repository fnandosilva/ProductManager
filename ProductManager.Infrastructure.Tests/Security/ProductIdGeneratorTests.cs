using FluentAssertions;
using ProductManager.Infrastructure.Security;
using ProductManger.Domain.Entities;

namespace ProductManager.Infrastructure.Tests.Security;

public class ProductIdGeneratorTests
{
    [Fact]
    public async Task GenerateNextIdAsync_ShouldReturnConfiguredStartingId()
    {
        using var context = TestDbContextFactory.Create();
        context.ProductIdSequences.Add(new ProductIdSequence { Id = 1, NextProductId = 100_000 });
        await context.SaveChangesAsync();

        var generator = new ProductIdGenerator(context);
        var id = await generator.GenerateNextIdAsync();

        id.Should().Be(100_000);
    }

    [Fact]
    public async Task GenerateNextIdAsync_CalledMultipleTimes_ShouldReturnSequentialIds()
    {
        using var context = TestDbContextFactory.Create();
        context.ProductIdSequences.Add(new ProductIdSequence { Id = 1, NextProductId = 100_000 });
        await context.SaveChangesAsync();

        var generator = new ProductIdGenerator(context);
        var id1 = await generator.GenerateNextIdAsync();
        var id2 = await generator.GenerateNextIdAsync();
        var id3 = await generator.GenerateNextIdAsync();

        id1.Should().Be(100_000);
        id2.Should().Be(100_001);
        id3.Should().Be(100_002);
    }

    [Fact]
    public async Task GenerateNextIdAsync_ShouldPersistIncrementedSequence()
    {
        using var context = TestDbContextFactory.Create();
        context.ProductIdSequences.Add(new ProductIdSequence { Id = 1, NextProductId = 100_000 });
        await context.SaveChangesAsync();

        var generator = new ProductIdGenerator(context);
        await generator.GenerateNextIdAsync();

        var sequence = await context.ProductIdSequences.FindAsync(1);
        sequence!.NextProductId.Should().Be(100_001);
    }

    [Fact]
    public async Task GenerateNextIdAsync_WhenExceedingMaxId_ShouldThrowInvalidOperationException()
    {
        using var context = TestDbContextFactory.Create();
        context.ProductIdSequences.Add(new ProductIdSequence { Id = 1, NextProductId = Product.MaxId + 1 });
        await context.SaveChangesAsync();

        var generator = new ProductIdGenerator(context);
        var act = () => generator.GenerateNextIdAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*range exhausted*");
    }

    [Fact]
    public async Task GenerateNextIdAsync_AtExactMaxId_ShouldSucceed()
    {
        using var context = TestDbContextFactory.Create();
        context.ProductIdSequences.Add(new ProductIdSequence { Id = 1, NextProductId = Product.MaxId });
        await context.SaveChangesAsync();

        var generator = new ProductIdGenerator(context);
        var id = await generator.GenerateNextIdAsync();

        id.Should().Be(Product.MaxId);
    }
}
