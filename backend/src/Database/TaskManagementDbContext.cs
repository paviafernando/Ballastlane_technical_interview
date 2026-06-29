using Microsoft.EntityFrameworkCore;
using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.Database;

public class TaskManagementDbContext : DbContext
{
    public TaskManagementDbContext(DbContextOptions<TaskManagementDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<UserTask> UserTasks => Set<UserTask>();
    public DbSet<TaskLog> TaskLogs => Set<TaskLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskManagementDbContext).Assembly);
    }
}
