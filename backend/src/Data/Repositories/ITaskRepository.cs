using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.Data.Repositories;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid taskId, Guid ownerUserId);
    Task<IReadOnlyList<TaskItem>> GetAllAsync(Guid ownerUserId);
    Task<TaskItem> CreateAsync(TaskItem task, Guid ownerUserId, TaskLog creationLog);
    Task<TaskItem> UpdateAsync(TaskItem task, TaskLog updateLog);
    Task DeleteAsync(TaskItem task, TaskLog deletionLog);
}
