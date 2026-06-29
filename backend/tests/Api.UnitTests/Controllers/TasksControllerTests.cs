using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManagementSystem.Api.Contracts;
using TaskManagementSystem.Api.Controllers;
using TaskManagementSystem.BusinessLogic.Exceptions;
using TaskManagementSystem.BusinessLogic.Tasks;
using TaskManagementSystem.Data.Entities;
using Xunit;

namespace TaskManagementSystem.Api.UnitTests.Controllers;

public class TasksControllerTests
{
    private readonly Mock<ITaskService> _taskServiceMock = new();
    private readonly TasksController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public TasksControllerTests()
    {
        _sut = new TasksController(_taskServiceMock.Object)
        {
            ControllerContext = ApiUnitTestHelpers.CreateAuthenticatedControllerContext("janedoe", _userId),
        };
    }

    private static TaskItem SomeTask() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Write report",
        Description = "Quarterly report",
        Status = TaskItemStatus.Pending,
        DueDate = new DateOnly(2026, 2, 1),
    };

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedWithTask()
    {
        var task = SomeTask();
        _taskServiceMock.Setup(s => s.CreateAsync(_userId, It.IsAny<CreateTaskRequest>())).ReturnsAsync(task);

        var result = await _sut.Create(new CreateTaskRequestDto("Write report", "Quarterly report", null, null));

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var body = created.Value.Should().BeOfType<TaskResponseDto>().Subject;
        body.Id.Should().Be(task.Id);
    }

    [Fact]
    public async Task Create_WithInvalidData_ReturnsBadRequest()
    {
        _taskServiceMock
            .Setup(s => s.CreateAsync(_userId, It.IsAny<CreateTaskRequest>()))
            .ThrowsAsync(new ValidationException("Title is required."));

        var result = await _sut.Create(new CreateTaskRequestDto("", null, null, null));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithTasksForCurrentUser()
    {
        var tasks = new List<TaskItem> { SomeTask() };
        _taskServiceMock.Setup(s => s.GetAllAsync(_userId)).ReturnsAsync(tasks);

        var result = await _sut.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<TaskResponseDto>>().Which.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_WhenTaskExists_ReturnsOk()
    {
        var task = SomeTask();
        _taskServiceMock.Setup(s => s.GetByIdAsync(_userId, task.Id)).ReturnsAsync(task);

        var result = await _sut.GetById(task.Id);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenTaskDoesNotExist_ReturnsNotFound()
    {
        var taskId = Guid.NewGuid();
        _taskServiceMock.Setup(s => s.GetByIdAsync(_userId, taskId)).ThrowsAsync(new NotFoundException("Task not found."));

        var result = await _sut.GetById(taskId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_WhenTaskExists_ReturnsOkWithUpdatedTask()
    {
        var task = SomeTask();
        _taskServiceMock
            .Setup(s => s.UpdateAsync(_userId, task.Id, It.IsAny<UpdateTaskRequest>()))
            .ReturnsAsync(task);

        var result = await _sut.Update(task.Id, new UpdateTaskRequestDto("Write report", null, TaskItemStatus.Pending, null));

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenTaskDoesNotExist_ReturnsNotFound()
    {
        var taskId = Guid.NewGuid();
        _taskServiceMock
            .Setup(s => s.UpdateAsync(_userId, taskId, It.IsAny<UpdateTaskRequest>()))
            .ThrowsAsync(new NotFoundException("Task not found."));

        var result = await _sut.Update(taskId, new UpdateTaskRequestDto("Title", null, TaskItemStatus.Pending, null));

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenTaskExists_ReturnsNoContent()
    {
        var taskId = Guid.NewGuid();

        var result = await _sut.Delete(taskId);

        result.Should().BeOfType<NoContentResult>();
        _taskServiceMock.Verify(s => s.DeleteAsync(_userId, taskId), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenTaskDoesNotExist_ReturnsNotFound()
    {
        var taskId = Guid.NewGuid();
        _taskServiceMock.Setup(s => s.DeleteAsync(_userId, taskId)).ThrowsAsync(new NotFoundException("Task not found."));

        var result = await _sut.Delete(taskId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
