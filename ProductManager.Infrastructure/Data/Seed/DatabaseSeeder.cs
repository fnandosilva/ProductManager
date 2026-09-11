using Microsoft.EntityFrameworkCore;
using ProductManager.Infrastructure.Data;
using ProductManager.Domain.Entities;

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
            Product.Create(100_005, "Adjustable Nose Pads", "Silicone replacement nose pads", 6.25m, 35),
            Product.Create(100_006, "Blue Light Filter Spray", "Anti-reflective coating refresher", 18.40m, 90),
            Product.Create(100_007, "Titanium Screw Kit", "Replacement hinge screws (12 pack)", 9.50m, 60),
            Product.Create(100_008, "Sport Strap", "Adjustable retainer for active wear", 14.25m, 120),
            Product.Create(100_009, "Polarized Clip-Ons", "Universal polarized clip-on sunglasses", 32.00m, 45),
            Product.Create(100_010, "Lens Pouches", "Soft microfiber pouches (5 pack)", 11.80m, 210),
            Product.Create(100_011, "Temple Tips", "Comfort silicone temple tips", 7.40m, 18),
            Product.Create(100_012, "UV Protection Spray", "UV400 lens protection spray", 16.90m, 75),
            Product.Create(100_013, "Reading Magnifier", "Pocket 3x optical magnifier", 21.00m, 40),
            Product.Create(100_014, "Frame Polish", "Gentle acetate frame polish", 13.50m, 55),
            Product.Create(100_015, "Nose Bridge Cushions", "Adhesive silicone cushions (8 pack)", 4.99m, 12),
            Product.Create(100_016, "Travel Lens Kit", "Cleaner, cloth, and case combo", 29.90m, 70),
            Product.Create(100_017, "Kids Frame Cord", "Breakaway cord for children's frames", 8.20m, 95),
            Product.Create(100_018, "Anti-Scratch Wipes", "Protective coating wipes (20 pack)", 10.75m, 160),
            Product.Create(100_019, "Optician Screwdriver", "Precision 5-in-1 screwdriver set", 19.99m, 28),
            Product.Create(100_020, "Display Stand", "Acrylic two-tier frame display", 27.50m, 22)
        };

        context.Products.AddRange(seedProducts);

        var sequence = await context.ProductIdSequences.SingleAsync(cancellationToken);
        sequence.NextProductId = 100_021;

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
