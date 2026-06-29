using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.BusinessLogic.Tasks;

public record UpdateTaskRequest(
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateOnly? DueDate);
