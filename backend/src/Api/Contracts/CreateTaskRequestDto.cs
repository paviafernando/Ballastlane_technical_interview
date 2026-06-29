using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.Api.Contracts;

public record CreateTaskRequestDto(
    string Title,
    string? Description,
    TaskItemStatus? Status,
    DateOnly? DueDate);
