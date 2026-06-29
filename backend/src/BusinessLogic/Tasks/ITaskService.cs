using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.BusinessLogic.Tasks;

public interface ITaskService
{
    Task<TaskItem> CreateAsync(Guid userId, CreateTaskRequest request);
    Task<TaskItem> GetByIdAsync(Guid userId, Guid taskId);
    Task<IReadOnlyList<TaskItem>> GetAllAsync(Guid userId);
    Task<TaskItem> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request);
    Task DeleteAsync(Guid userId, Guid taskId);
}
