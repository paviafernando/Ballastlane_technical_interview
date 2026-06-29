using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskManagementSystem.Data.Entities;
using TaskManagementSystem.Database.Repositories;
using TaskManagementSystem.Database.UnitTests.Fixtures;
using Xunit;

namespace TaskManagementSystem.Database.UnitTests.Repositories;

[Collection(nameof(DatabaseTestCollection))]
public class TaskRepositoryTests : IAsyncLifetime
{
    private readonly TaskManagementDbContext _dbContext;
    private readonly TaskRepository _sut;
    private Guid _ownerUserId;
    private Guid _otherUserId;

    public TaskRepositoryTests()
    {
        _dbContext = DatabaseTestFixture.CreateContext();
        _sut = new TaskRepository(_dbContext);
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE task_logs, users_tasks, tasks, users RESTART IDENTITY CASCADE;");

        _ownerUserId = Guid.NewGuid();
        _otherUserId = Guid.NewGuid();
        _dbContext.Users.AddRange(NewUser(_ownerUserId, "owner"), NewUser(_otherUserId, "otheruser"));
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }

    private static User NewUser(Guid id, string username) => new()
    {
        Id = id,
        Name = "Test",
        LastName = "User",
        Username = username,
        PasswordHash = "hashed",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    private static TaskItem NewTask(string title = "Write report") => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Description = "Quarterly report",
        Status = TaskItemStatus.Pending,
        DueDate = new DateOnly(2026, 2, 1),
    };

    private TaskLog NewLog(Guid taskId, Guid createdBy, string comment = "Task created.") => new()
    {
        Id = Guid.NewGuid(),
        TaskId = taskId,
        OldValue = null,
        NewValue = "{}",
        Comment = comment,
        CreatedBy = createdBy,
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task CreateAsync_PersistsTaskAndOwnershipAndLog()
    {
        var task = NewTask();

        await _sut.CreateAsync(task, _ownerUserId, NewLog(task.Id, _ownerUserId));

        using var verificationContext = DatabaseTestFixture.CreateContext();
        var persistedTask = await verificationContext.Tasks.FindAsync(task.Id);
        var ownership = await verificationContext.UserTasks.SingleOrDefaultAsync(ut => ut.TaskId == task.Id);
        var log = await verificationContext.TaskLogs.SingleOrDefaultAsync(l => l.TaskId == task.Id);

        persistedTask.Should().NotBeNull();
        persistedTask!.Title.Should().Be("Write report");
        ownership.Should().NotBeNull();
        ownership!.UserId.Should().Be(_ownerUserId);
        log.Should().NotBeNull();
        log!.Comment.Should().Be("Task created.");
    }

    [Fact]
    public async Task CreateAsync_PersistsInProgressStatusAsTextWithSpace()
    {
        var task = NewTask();
        task.Status = TaskItemStatus.InProgress;

        await _sut.CreateAsync(task, _ownerUserId, NewLog(task.Id, _ownerUserId));

        await using var connection = new Npgsql.NpgsqlConnection(DatabaseTestFixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT status FROM tasks WHERE id = @id";
        command.Parameters.AddWithValue("id", task.Id);
        var rawStatus = (string)(await command.ExecuteScalarAsync())!;

        rawStatus.Should().Be("In Progress");
    }

    [Fact]
    public async Task GetByIdAsync_WhenOwnedByUser_ReturnsTask()
    {
        var task = NewTask();
        await _sut.CreateAsync(task, _ownerUserId, NewLog(task.Id, _ownerUserId));
        _dbContext.ChangeTracker.Clear();

        var result = await _sut.GetByIdAsync(task.Id, _ownerUserId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOwnedByAnotherUser_ReturnsNull()
    {
        var task = NewTask();
        await _sut.CreateAsync(task, _ownerUserId, NewLog(task.Id, _ownerUserId));
        _dbContext.ChangeTracker.Clear();

        var result = await _sut.GetByIdAsync(task.Id, _otherUserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyTasksOwnedByThatUser()
    {
        var ownedTask = NewTask("Owned task");
        var otherTask = NewTask("Other user's task");
        await _sut.CreateAsync(ownedTask, _ownerUserId, NewLog(ownedTask.Id, _ownerUserId));
        await _sut.CreateAsync(otherTask, _otherUserId, NewLog(otherTask.Id, _otherUserId));
        _dbContext.ChangeTracker.Clear();

        var result = await _sut.GetAllAsync(_ownerUserId);

        result.Should().ContainSingle(t => t.Id == ownedTask.Id);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChangesAndAddsLog()
    {
        var task = NewTask();
        await _sut.CreateAsync(task, _ownerUserId, NewLog(task.Id, _ownerUserId));
        _dbContext.ChangeTracker.Clear();

        task.Title = "Updated title";
        await _sut.UpdateAsync(task, NewLog(task.Id, _ownerUserId, "Task updated."));

        using var verificationContext = DatabaseTestFixture.CreateContext();
        var persistedTask = await verificationContext.Tasks.FindAsync(task.Id);
        var logCount = await verificationContext.TaskLogs.CountAsync(l => l.TaskId == task.Id);

        persistedTask!.Title.Should().Be("Updated title");
        logCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTaskAndOwnershipButKeepsLogs()
    {
        var task = NewTask();
        await _sut.CreateAsync(task, _ownerUserId, NewLog(task.Id, _ownerUserId));
        _dbContext.ChangeTracker.Clear();

        await _sut.DeleteAsync(task, NewLog(task.Id, _ownerUserId, "Task deleted."));

        using var verificationContext = DatabaseTestFixture.CreateContext();
        var persistedTask = await verificationContext.Tasks.FindAsync(task.Id);
        var ownership = await verificationContext.UserTasks.SingleOrDefaultAsync(ut => ut.TaskId == task.Id);
        var logCount = await verificationContext.TaskLogs.CountAsync(l => l.TaskId == task.Id);

        persistedTask.Should().BeNull();
        ownership.Should().BeNull();
        logCount.Should().Be(2);
    }
}
