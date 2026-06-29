namespace TaskManagementSystem.Data.Entities;

/// <summary>
/// Bridge table representing the single current owner of a task. Kept as a bridge (rather
/// than a plain FK on TaskItem) so task reassignment history can be preserved later.
/// </summary>
public class UserTask
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TaskId { get; set; }
}
