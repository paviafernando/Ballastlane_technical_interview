using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManagementSystem.BusinessLogic.Exceptions;
using TaskManagementSystem.Data.Entities;
using TaskManagementSystem.Data.Repositories;

namespace TaskManagementSystem.BusinessLogic.Tasks;

public class TaskService : ITaskService
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ITaskRepository _taskRepository;
    private readonly TimeProvider _timeProvider;

    public TaskService(ITaskRepository taskRepository, TimeProvider timeProvider)
    {
        _taskRepository = taskRepository;
        _timeProvider = timeProvider;
    }

    public async Task<TaskItem> CreateAsync(Guid userId, CreateTaskRequest request)
    {
        ValidateTitle(request.Title);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Status = request.Status ?? TaskItemStatus.Pending,
            DueDate = request.DueDate,
        };

        var log = BuildLog(task.Id, oldValue: null, newValue: task, comment: "Task created.", userId);

        return await _taskRepository.CreateAsync(task, userId, log);
    }

    public async Task<TaskItem> GetByIdAsync(Guid userId, Guid taskId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, userId);
        return task ?? throw new NotFoundException("Task not found.");
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(Guid userId) =>
        await _taskRepository.GetAllAsync(userId);

    public async Task<TaskItem> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request)
    {
        ValidateTitle(request.Title);

        var task = await _taskRepository.GetByIdAsync(taskId, userId)
            ?? throw new NotFoundException("Task not found.");

        var oldValueJson = JsonSerializer.Serialize(task, SnapshotJsonOptions);

        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.DueDate = request.DueDate;

        var log = BuildLogFromJson(taskId, oldValueJson, JsonSerializer.Serialize(task, SnapshotJsonOptions), "Task updated.", userId);

        return await _taskRepository.UpdateAsync(task, log);
    }

    public async Task DeleteAsync(Guid userId, Guid taskId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, userId)
            ?? throw new NotFoundException("Task not found.");

        var log = BuildLog(taskId, oldValue: task, newValue: null, comment: "Task deleted.", userId);

        await _taskRepository.DeleteAsync(task, log);
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Title is required.");
        }
    }

    private TaskLog BuildLog(Guid taskId, TaskItem? oldValue, TaskItem? newValue, string comment, Guid userId) =>
        BuildLogFromJson(
            taskId,
            oldValue is null ? null : JsonSerializer.Serialize(oldValue, SnapshotJsonOptions),
            newValue is null ? null : JsonSerializer.Serialize(newValue, SnapshotJsonOptions),
            comment,
            userId);

    private TaskLog BuildLogFromJson(Guid taskId, string? oldValueJson, string? newValueJson, string comment, Guid userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            OldValue = oldValueJson,
            NewValue = newValueJson,
            Comment = comment,
            CreatedBy = userId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        };
}
