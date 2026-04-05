using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class CommandConfiguration : IEntityTypeConfiguration<Command>
{
    public void Configure(EntityTypeBuilder<Command> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BroadcasterId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Permission).IsRequired().HasMaxLength(20).HasDefaultValue("everyone");
        builder.Property(e => e.Type).IsRequired().HasMaxLength(20).HasDefaultValue("text");
        builder.Property(e => e.Response).HasMaxLength(2000);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.PipelineJson).HasColumnType("jsonb");

        // JSON columns
        builder.Property(e => e.Responses).HasColumnType("jsonb");
        builder.Property(e => e.Aliases).HasColumnType("jsonb");

        // Relationships
        builder.HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => new { e.Name, e.BroadcasterId })
            .IsUnique()
            .HasDatabaseName("IX_Command_Name_BroadcasterId");

        // Soft delete filter
        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
