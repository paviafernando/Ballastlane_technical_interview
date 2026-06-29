using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.Api.Contracts;

public record UpdateTaskRequestDto(
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateOnly? DueDate);
