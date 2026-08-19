using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductManager.Infrastructure.Auth;
using ProductManager.Infrastructure.Data;
using ProductManager.Infrastructure.Products;
using ProductManager.Infrastructure.Security;
using ProductManger.Domain.Repositories;
using ProductManger.Domain.Services;

namespace ProductManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var useInMemoryDatabase = configuration.GetValue<bool>("Database:UseInMemoryDatabase");

        services.AddDbContext<AppDbContext>(options =>
        {
            if (useInMemoryDatabase)
            {
                options.UseInMemoryDatabase(configuration["Database:InMemoryDatabaseName"] ?? "ProductManagerDb");
                return;
            }

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductIdGenerator, ProductIdGenerator>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
