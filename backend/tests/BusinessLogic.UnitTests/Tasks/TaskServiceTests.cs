using FluentAssertions;
using Moq;
using TaskManagementSystem.BusinessLogic.Exceptions;
using TaskManagementSystem.BusinessLogic.Tasks;
using TaskManagementSystem.Data.Entities;
using TaskManagementSystem.Data.Repositories;
using Xunit;

namespace TaskManagementSystem.BusinessLogic.UnitTests.Tasks;

public class TaskServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ITaskRepository> _taskRepositoryMock = new();
    private readonly Mock<TimeProvider> _timeProviderMock = new();
    private readonly TaskService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public TaskServiceTests()
    {
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(Now);
        _sut = new TaskService(_taskRepositoryMock.Object, _timeProviderMock.Object);

        _taskRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<TaskItem>(), It.IsAny<Guid>(), It.IsAny<TaskLog>()))
            .ReturnsAsync((TaskItem task, Guid _, TaskLog _) => task);
        _taskRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<TaskLog>()))
            .ReturnsAsync((TaskItem task, TaskLog _) => task);
    }

    private static CreateTaskRequest ValidCreateRequest() =>
        new("Write report", "Quarterly report", TaskItemStatus.Pending, new DateOnly(2026, 2, 1));

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsTaskWithGeneratedId()
    {
        var result = await _sut.CreateAsync(_userId, ValidCreateRequest());

        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateAsync_WithoutExplicitStatus_DefaultsToPending()
    {
        var request = ValidCreateRequest() with { Status = null };

        var result = await _sut.CreateAsync(_userId, request);

        result.Status.Should().Be(TaskItemStatus.Pending);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyTitle_ThrowsValidationException()
    {
        var request = ValidCreateRequest() with { Title = "" };

        var act = () => _sut.CreateAsync(_userId, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_PassesCreationLogWithNullOldValueAndOwnerAsCreatedBy()
    {
        TaskLog? capturedLog = null;
        _taskRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<TaskItem>(), _userId, It.IsAny<TaskLog>()))
            .Callback<TaskItem, Guid, TaskLog>((_, _, log) => capturedLog = log)
            .ReturnsAsync((TaskItem task, Guid _, TaskLog _) => task);

        await _sut.CreateAsync(_userId, ValidCreateRequest());

        capturedLog.Should().NotBeNull();
        capturedLog!.OldValue.Should().BeNull();
        capturedLog.NewValue.Should().NotBeNullOrEmpty();
        capturedLog.CreatedBy.Should().Be(_userId);
        capturedLog.CreatedAt.Should().Be(Now.UtcDateTime);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskBelongsToUser_ReturnsTask()
    {
        var task = new TaskItem { Id = Guid.NewGuid(), Title = "Write report" };
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, _userId)).ReturnsAsync(task);

        var result = await _sut.GetByIdAsync(_userId, task.Id);

        result.Id.Should().Be(task.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskDoesNotBelongToUserOrDoesNotExist_ThrowsNotFoundException()
    {
        var taskId = Guid.NewGuid();
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId, _userId)).ReturnsAsync((TaskItem?)null);

        var act = () => _sut.GetByIdAsync(_userId, taskId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTasksFromRepositoryForThatUser()
    {
        var tasks = new List<TaskItem> { new() { Id = Guid.NewGuid(), Title = "A" } };
        _taskRepositoryMock.Setup(r => r.GetAllAsync(_userId)).ReturnsAsync(tasks);

        var result = await _sut.GetAllAsync(_userId);

        result.Should().BeEquivalentTo(tasks);
    }

    [Fact]
    public async Task UpdateAsync_WhenTaskBelongsToUser_UpdatesAndReturnsTask()
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Old title",
            Description = "Old description",
            Status = TaskItemStatus.Pending,
            DueDate = new DateOnly(2026, 2, 1),
        };
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, _userId)).ReturnsAsync(task);

        var request = new UpdateTaskRequest("New title", "New description", TaskItemStatus.InProgress, new DateOnly(2026, 3, 1));
        var result = await _sut.UpdateAsync(_userId, task.Id, request);

        result.Title.Should().Be("New title");
        result.Description.Should().Be("New description");
        result.Status.Should().Be(TaskItemStatus.InProgress);
        result.DueDate.Should().Be(new DateOnly(2026, 3, 1));
    }

    [Fact]
    public async Task UpdateAsync_PassesUpdateLogWithOldAndNewSnapshots()
    {
        var task = new TaskItem { Id = Guid.NewGuid(), Title = "Old title", Status = TaskItemStatus.Pending };
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, _userId)).ReturnsAsync(task);

        TaskLog? capturedLog = null;
        _taskRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<TaskLog>()))
            .Callback<TaskItem, TaskLog>((_, log) => capturedLog = log)
            .ReturnsAsync((TaskItem t, TaskLog _) => t);

        var request = new UpdateTaskRequest("New title", null, TaskItemStatus.InProgress, null);
        await _sut.UpdateAsync(_userId, task.Id, request);

        capturedLog.Should().NotBeNull();
        capturedLog!.OldValue.Should().Contain("Old title");
        capturedLog.NewValue.Should().Contain("New title");
        capturedLog.CreatedBy.Should().Be(_userId);
    }

    [Fact]
    public async Task UpdateAsync_WhenTaskDoesNotBelongToUser_ThrowsNotFoundException()
    {
        var taskId = Guid.NewGuid();
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId, _userId)).ReturnsAsync((TaskItem?)null);

        var act = () => _sut.UpdateAsync(_userId, taskId, new UpdateTaskRequest("Title", null, TaskItemStatus.Pending, null));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyTitle_ThrowsValidationException()
    {
        var task = new TaskItem { Id = Guid.NewGuid(), Title = "Old title" };
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, _userId)).ReturnsAsync(task);

        var act = () => _sut.UpdateAsync(_userId, task.Id, new UpdateTaskRequest("", null, TaskItemStatus.Pending, null));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskBelongsToUser_DeletesTask()
    {
        var task = new TaskItem { Id = Guid.NewGuid(), Title = "Write report" };
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, _userId)).ReturnsAsync(task);

        await _sut.DeleteAsync(_userId, task.Id);

        _taskRepositoryMock.Verify(r => r.DeleteAsync(task, It.IsAny<TaskLog>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PassesDeletionLogWithNullNewValue()
    {
        var task = new TaskItem { Id = Guid.NewGuid(), Title = "Write report" };
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, _userId)).ReturnsAsync(task);

        TaskLog? capturedLog = null;
        _taskRepositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<TaskItem>(), It.IsAny<TaskLog>()))
            .Callback<TaskItem, TaskLog>((_, log) => capturedLog = log);

        await _sut.DeleteAsync(_userId, task.Id);

        capturedLog.Should().NotBeNull();
        capturedLog!.NewValue.Should().BeNull();
        capturedLog.OldValue.Should().Contain("Write report");
        capturedLog.CreatedBy.Should().Be(_userId);
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskDoesNotBelongToUser_ThrowsNotFoundException()
    {
        var taskId = Guid.NewGuid();
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId, _userId)).ReturnsAsync((TaskItem?)null);

        var act = () => _sut.DeleteAsync(_userId, taskId);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
