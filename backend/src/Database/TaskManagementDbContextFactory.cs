using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskManagementSystem.Database;

/// <summary>
/// Used by `dotnet ef` design-time commands (migrations) to construct the DbContext
/// without needing the Api project as a startup project. Connection details match the
/// local dev Postgres container in docker-compose.yml (no real secrets involved).
/// </summary>
public class TaskManagementDbContextFactory : IDesignTimeDbContextFactory<TaskManagementDbContext>
{
    public TaskManagementDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TaskManagementDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5433;Database=task_management_system;Username=taskuser;Password=taskpass");

        return new TaskManagementDbContext(optionsBuilder.Options);
    }
}
