using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.Database.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.Name).HasColumnName("name").IsRequired();
        builder.Property(u => u.LastName).HasColumnName("lastname").IsRequired();
        builder.Property(u => u.Username).HasColumnName("username").IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
        builder.Property(u => u.Birthday).HasColumnName("birthday");
        builder.Property(u => u.FailedLoginAttempts).HasColumnName("failed_login_attempts").IsRequired();
        builder.Property(u => u.LockedUntil).HasColumnName("locked_until");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(u => u.Username).IsUnique();
    }
}
