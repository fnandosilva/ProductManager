using Microsoft.EntityFrameworkCore;
using ProductManager.Infrastructure.Data;
using ProductManger.Domain.Entities;

namespace ProductManager.Infrastructure.Data.Seed;

/// <summary>
/// Owns exactly one job: inserting data. Schema changes are <see cref="DatabaseMigrator"/>'s job.
/// Seeding itself is split into two distinct concerns that need very different treatment in
/// production:
/// <list type="bullet">
/// <item><see cref="SeedRequiredDataAsync"/> — data the app cannot function without (the
/// <see cref="ProductIdSequence"/> counter row). Must run in every environment.</item>
/// <item><see cref="SeedSampleDataAsync"/> — sample products and a demo login for a friendly
/// out-of-the-box experience. Must NOT run against a real production database (sample products
/// and a well-known demo password have no place there) — callers gate this behind
/// <c>Database:SeedSampleData</c>, which only defaults to <c>true</c> in Development.</item>
/// </list>
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedRequiredDataAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await context.ProductIdSequences.AnyAsync(cancellationToken))
        {
            context.ProductIdSequences.Add(new ProductIdSequence
            {
                Id = 1,
                NextProductId = Product.MinId
            });

            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public static async Task SeedSampleDataAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        // Seeded independently of the products early-return below so it still runs against a
        // database that already has products (e.g. an existing persisted volume from before
        // this demo login existed), instead of only on a completely empty database.
        await SeedDemoUserAsync(context, cancellationToken);

        if (await context.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var seedProducts = new[]
        {
            Product.Create(100_001, "Zeiss Lens Cleaner", "Professional lens cleaning solution", 12.99m, 150),
            Product.Create(100_002, "Premium Eyeglass Case", "Hard-shell protective case", 24.50m, 80),
            Product.Create(100_003, "Anti-Fog Wipes", "Single-use anti-fog lens wipes (30 pack)", 8.75m, 200),
            Product.Create(100_004, "Microfiber Cloth", "Ultra-soft cleaning cloth", 5.99m, 500),
            Product.Create(100_005, "Adjustable Nose Pads", "Silicone replacement nose pads", 6.25m, 35)
        };

        context.Products.AddRange(seedProducts);

        var sequence = await context.ProductIdSequences.SingleAsync(cancellationToken);
        sequence.NextProductId = 100_006;

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Seeds a single demo login so the app is usable immediately after `docker compose up` —
    /// without it, the Angular login page has no self-service registration and no way to obtain
    /// credentials short of calling POST /api/auth/register directly (see README).
    /// </summary>
    private static async Task SeedDemoUserAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        const string demoUsername = "demo";
        const string demoEmail = "demo@productmanager.local";
        const string demoPassword = "Demo@1234";

        context.Users.Add(User.Create(demoUsername, demoEmail, BCrypt.Net.BCrypt.HashPassword(demoPassword)));
        await context.SaveChangesAsync(cancellationToken);

        Console.WriteLine("========================================================");
        Console.WriteLine(" Demo login seeded (see README.md for details):");
        Console.WriteLine($"   Username: {demoUsername}");
        Console.WriteLine($"   Password: {demoPassword}");
        Console.WriteLine("========================================================");
    }
}
