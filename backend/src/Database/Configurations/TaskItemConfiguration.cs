using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.Database.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.Title).HasColumnName("title").IsRequired();
        builder.Property(t => t.Description).HasColumnName("description");
        builder.Property(t => t.DueDate).HasColumnName("due_date");

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion(
                status => StatusToString(status),
                value => StatusFromString(value))
            .IsRequired();
    }

    private static string StatusToString(TaskItemStatus status) => status switch
    {
        TaskItemStatus.InProgress => "In Progress",
        _ => status.ToString(),
    };

    private static TaskItemStatus StatusFromString(string value) => value switch
    {
        "In Progress" => TaskItemStatus.InProgress,
        _ => Enum.Parse<TaskItemStatus>(value),
    };
}
