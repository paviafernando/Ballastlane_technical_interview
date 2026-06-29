using Microsoft.EntityFrameworkCore;
using TaskManagementSystem.Data.Entities;
using TaskManagementSystem.Data.Repositories;

namespace TaskManagementSystem.Database.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly TaskManagementDbContext _dbContext;

    public TaskRepository(TaskManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid taskId, Guid ownerUserId)
    {
        var isOwner = await _dbContext.UserTasks
            .AnyAsync(ut => ut.TaskId == taskId && ut.UserId == ownerUserId);

        return isOwner ? await _dbContext.Tasks.SingleOrDefaultAsync(t => t.Id == taskId) : null;
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(Guid ownerUserId)
    {
        var ownedTaskIds = _dbContext.UserTasks
            .Where(ut => ut.UserId == ownerUserId)
            .Select(ut => ut.TaskId);

        return await _dbContext.Tasks
            .Where(t => ownedTaskIds.Contains(t.Id))
            .ToListAsync();
    }

    public async Task<TaskItem> CreateAsync(TaskItem task, Guid ownerUserId, TaskLog creationLog)
    {
        _dbContext.Tasks.Add(task);
        _dbContext.UserTasks.Add(new UserTask { Id = Guid.NewGuid(), UserId = ownerUserId, TaskId = task.Id });
        _dbContext.TaskLogs.Add(creationLog);

        await _dbContext.SaveChangesAsync();
        return task;
    }

    public async Task<TaskItem> UpdateAsync(TaskItem task, TaskLog updateLog)
    {
        _dbContext.Tasks.Update(task);
        _dbContext.TaskLogs.Add(updateLog);

        await _dbContext.SaveChangesAsync();
        return task;
    }

    public async Task DeleteAsync(TaskItem task, TaskLog deletionLog)
    {
        _dbContext.Tasks.Remove(task);
        _dbContext.TaskLogs.Add(deletionLog);

        await _dbContext.SaveChangesAsync();
    }
}
