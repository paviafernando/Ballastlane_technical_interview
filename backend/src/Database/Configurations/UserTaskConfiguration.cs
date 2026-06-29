using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.Database.Configurations;

public class UserTaskConfiguration : IEntityTypeConfiguration<UserTask>
{
    public void Configure(EntityTypeBuilder<UserTask> builder)
    {
        builder.ToTable("users_tasks");

        builder.HasKey(ut => ut.Id);
        builder.Property(ut => ut.Id).HasColumnName("id");
        builder.Property(ut => ut.UserId).HasColumnName("id_user").IsRequired();
        builder.Property(ut => ut.TaskId).HasColumnName("id_task").IsRequired();

        builder.HasIndex(ut => ut.TaskId).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ut => ut.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<TaskItem>()
            .WithMany()
            .HasForeignKey(ut => ut.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
