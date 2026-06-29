namespace TaskManagementSystem.Data.Entities;

/// <summary>
/// Audit trail entry for a task change. TaskId is deliberately not a real foreign key —
/// the log must survive deletion of the task it refers to.
/// </summary>
public class TaskLog
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Comment { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
