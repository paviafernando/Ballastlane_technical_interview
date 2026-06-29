using Microsoft.EntityFrameworkCore;
using TaskManagementSystem.Database;
using Xunit;

namespace TaskManagementSystem.Database.UnitTests.Fixtures;

/// <summary>
/// Integration tests in this project run against a real Postgres instance (the
/// task_management_system_test database from docker-compose.yml), not an in-memory provider,
/// so they catch real Npgsql/EF Core behavior. Postgres must be running locally
/// (docker compose up -d) before executing these tests.
/// </summary>
public class DatabaseTestFixture : IAsyncLifetime
{
    public const string ConnectionString =
        "Host=localhost;Port=5433;Database=task_management_system_test;Username=taskuser;Password=taskpass";

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public static TaskManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new TaskManagementDbContext(options);
    }
}

[CollectionDefinition(nameof(DatabaseTestCollection))]
public class DatabaseTestCollection : ICollectionFixture<DatabaseTestFixture>
{
}
