using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagementSystem.BusinessLogic.Tasks;
using TaskManagementSystem.BusinessLogic.Users;
using TaskManagementSystem.Data.Entities;
using TaskManagementSystem.Database;

namespace TaskManagementSystem.Api.Seeding;

/// <summary>
/// Seeds a demo user and a handful of demo tasks on first run, so the app has usable
/// data/credentials out of the box (per the assignment's "seeded data/credentials for demo
/// purposes" requirement). Goes through the normal BusinessLogic services rather than raw
/// inserts, so seeded data gets the same validation and audit trail as everything else.
/// Idempotent: skipped entirely if any user already exists.
/// </summary>
public static class DatabaseSeeder
{
    public const string DemoUsername = "demo";
    public const string DemoPassword = "Demo12345!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var dbContext = services.GetRequiredService<TaskManagementDbContext>();
        if (await dbContext.Users.AnyAsync())
        {
            return;
        }

        var userRegistrationService = services.GetRequiredService<IUserRegistrationService>();
        var taskService = services.GetRequiredService<ITaskService>();

        var demoUser = await userRegistrationService.RegisterAsync(
            new RegisterUserRequest("Demo", "User", DemoUsername, DemoPassword, null));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var demoTasks = new[]
        {
            new CreateTaskRequest(
                "Set up project repository",
                "Initialize the monorepo, Docker Compose, and VS Code F5 scaffolding.",
                TaskItemStatus.Completed,
                today.AddDays(-7)),
            new CreateTaskRequest(
                "Design database schema",
                "Model users, tasks, ownership (users_tasks), and audit logging (task_logs).",
                TaskItemStatus.Completed,
                today.AddDays(-5)),
            new CreateTaskRequest(
                "Implement authentication",
                "JWT login with sequential account lockout on repeated failed attempts.",
                TaskItemStatus.InProgress,
                today.AddDays(2)),
            new CreateTaskRequest(
                "Build task management UI",
                "React + TypeScript frontend for the task CRUD.",
                TaskItemStatus.Pending,
                today.AddDays(14)),
            new CreateTaskRequest(
                "Write GenAI tools writeup",
                "Document prompts, sample output, and validation for the GenAI tools section.",
                TaskItemStatus.Blocked,
                today.AddDays(20)),
        };

        foreach (var request in demoTasks)
        {
            await taskService.CreateAsync(demoUser.Id, request);
        }
    }
}
