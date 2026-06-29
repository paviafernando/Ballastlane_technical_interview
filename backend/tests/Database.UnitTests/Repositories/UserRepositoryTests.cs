using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskManagementSystem.Data.Entities;
using TaskManagementSystem.Database.Repositories;
using TaskManagementSystem.Database.UnitTests.Fixtures;
using Xunit;

namespace TaskManagementSystem.Database.UnitTests.Repositories;

[Collection(nameof(DatabaseTestCollection))]
public class UserRepositoryTests : IAsyncLifetime
{
    private readonly TaskManagementDbContext _dbContext;
    private readonly UserRepository _sut;

    public UserRepositoryTests()
    {
        _dbContext = DatabaseTestFixture.CreateContext();
        _sut = new UserRepository(_dbContext);
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE users RESTART IDENTITY CASCADE;");
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }

    private static User NewUser(string username = "janedoe") => new()
    {
        Id = Guid.NewGuid(),
        Name = "Jane",
        LastName = "Doe",
        Username = username,
        PasswordHash = "hashed-password",
        Birthday = new DateOnly(1990, 1, 1),
        FailedLoginAttempts = 0,
        LockedUntil = null,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task AddAsync_PersistsUserToDatabase()
    {
        var user = NewUser();

        await _sut.AddAsync(user);

        using var verificationContext = DatabaseTestFixture.CreateContext();
        var persisted = await verificationContext.Users.FindAsync(user.Id);

        persisted.Should().NotBeNull();
        persisted!.Username.Should().Be("janedoe");
    }

    [Fact]
    public async Task AddAsync_ReturnsTheSameUser()
    {
        var user = NewUser();

        var result = await _sut.AddAsync(user);

        result.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByUsernameAsync_WithExistingUsername_ReturnsUser()
    {
        var user = NewUser("existinguser");
        await _sut.AddAsync(user);
        _dbContext.ChangeTracker.Clear();

        var result = await _sut.GetByUsernameAsync("existinguser");

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByUsernameAsync_WithNonExistingUsername_ReturnsNull()
    {
        var result = await _sut.GetByUsernameAsync("doesnotexist");

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_PersistsChangesToFailedLoginAttemptsAndLockedUntil()
    {
        var user = NewUser();
        await _sut.AddAsync(user);
        _dbContext.ChangeTracker.Clear();

        var lockedUntil = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        user.FailedLoginAttempts = 3;
        user.LockedUntil = lockedUntil;
        await _sut.UpdateAsync(user);

        using var verificationContext = DatabaseTestFixture.CreateContext();
        var persisted = await verificationContext.Users.FindAsync(user.Id);

        persisted.Should().NotBeNull();
        persisted!.FailedLoginAttempts.Should().Be(3);
        persisted.LockedUntil.Should().Be(lockedUntil);
    }
}
