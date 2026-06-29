using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.Api.Contracts;

public record TaskResponseDto(Guid Id, string Title, string? Description, TaskItemStatus Status, DateOnly? DueDate)
{
    public static TaskResponseDto FromEntity(TaskItem task) =>
        new(task.Id, task.Title, task.Description, task.Status, task.DueDate);
}
