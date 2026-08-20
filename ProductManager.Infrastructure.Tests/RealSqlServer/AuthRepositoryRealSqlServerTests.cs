using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProductManager.Infrastructure.Auth;
using ProductManager.Infrastructure.Data;
using ProductManager.Infrastructure.Tests.Security;
using ProductManager.Domain.Entities;

namespace ProductManager.Infrastructure.Tests.RealSqlServer;

/// <summary>
/// Proves string-comparison behavior that only a real relational engine's collation can exercise.
/// <see cref="AuthRepository.GetByUsernameAsync"/> and the unique index on <c>Users.Username</c>
/// (<c>UserConfiguration</c>) both compare/enforce with a plain <c>==</c>/no explicit collation —
/// under SQL Server's default case-insensitive collation that makes "JohnDoe" and "johndoe"
/// collide, but under EF Core's InMemory provider (ordinal, case-sensitive CLR string comparison)
/// they never would. Every other <c>AuthRepository</c> test in the suite runs on InMemory and
/// would happily let "JohnDoe" register as a "different" user from an existing "johndoe" — the
/// opposite of what actually happens once this hits a real SQL Server. Requires a real SQL
/// Server; see <see cref="RealSqlServerTestDatabase"/> and the README's "Testing" section. Skips
/// automatically (rather than failing) when none is reachable.
/// </summary>
public class AuthRepositoryRealSqlServerTests : IAsyncLifetime
{
    private RealSqlServerTestDatabase? _database;

    public async Task InitializeAsync()
    {
        _database = await RealSqlServerTestDatabase.TryCreateAsync();
        if (_database is null)
        {
            return;
        }

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task AddAsync_WithAUsernameDifferingOnlyByCase_ShouldViolateTheUniqueIndexOnRealSqlServer()
    {
        SkipIfNoRealSqlServer();

        await using var writeContext = CreateContext();
        writeContext.Database.IsRelational().Should().BeTrue(
            "this test only proves anything against the real collation-driven unique index, not InMemory's ordinal comparison");

        var repository = new AuthRepository(writeContext);
        await repository.AddAsync(User.Create("johndoe", "john@example.com", "hash-1"));

        var act = async () => await repository.AddAsync(User.Create("JohnDoe", "jane@example.com", "hash-2"));

        // On InMemory, "johndoe" and "JohnDoe" are ordinally distinct, so the equivalent
        // AddAsync call there succeeds instead of throwing — see AuthRepositoryTests for the
        // (deliberately different) InMemory-only duplicate-email/duplicate-username coverage.
        await act.Should().ThrowAsync<DbUpdateException>(
            "SQL Server's default case-insensitive collation treats 'johndoe' and 'JohnDoe' as the same value " +
            "for the unique index on Username, unlike InMemory's ordinal string comparison");
    }

    [SkippableFact]
    public async Task GetByUsernameAsync_WithDifferentCasing_ShouldStillFindTheUserOnRealSqlServer()
    {
        SkipIfNoRealSqlServer();

        await using (var writeContext = CreateContext())
        {
            await new AuthRepository(writeContext).AddAsync(User.Create("johndoe", "john@example.com", "hash-1"));
        }

        await using var readContext = CreateContext();
        var stored = await new AuthRepository(readContext).GetByUsernameAsync("JOHNDOE");

        stored.Should().NotBeNull(
            "GetByUsernameAsync does a plain '==' comparison with no explicit collation; on real SQL Server " +
            "that resolves case-insensitively, so callers (e.g. login-by-username flows) must not assume " +
            "InMemory's case-sensitive behavior reflects production");
        stored!.Username.Should().Be("johndoe", "the originally-stored casing is preserved for display");
    }

    private void SkipIfNoRealSqlServer()
    {
        Skip.If(_database is null,
            "No real SQL Server is reachable (tried SQL_TEST_CONNECTION_STRING, the docker-compose " +
            "sqlserver service on localhost,1433, and LocalDB). Start one to run this test — see README.");
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_database!.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }
}
