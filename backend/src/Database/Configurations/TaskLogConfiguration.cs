using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.Database.Configurations;

public class TaskLogConfiguration : IEntityTypeConfiguration<TaskLog>
{
    public void Configure(EntityTypeBuilder<TaskLog> builder)
    {
        builder.ToTable("task_logs");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");

        // TaskId is deliberately not a foreign key: this audit trail must survive deletion
        // of the task it refers to.
        builder.Property(l => l.TaskId).HasColumnName("task_id").IsRequired();
        builder.HasIndex(l => l.TaskId);

        builder.Property(l => l.OldValue).HasColumnName("old_value").HasColumnType("jsonb");
        builder.Property(l => l.NewValue).HasColumnName("new_value").HasColumnType("jsonb");
        builder.Property(l => l.Comment).HasColumnName("comment").IsRequired();
        builder.Property(l => l.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(l => l.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(l => l.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
