using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.BusinessLogic.Tasks;

public record CreateTaskRequest(
    string Title,
    string? Description,
    TaskItemStatus? Status,
    DateOnly? DueDate);
