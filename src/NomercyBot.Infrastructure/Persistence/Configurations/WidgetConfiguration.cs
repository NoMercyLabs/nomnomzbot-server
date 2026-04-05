using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class WidgetConfiguration : IEntityTypeConfiguration<Widget>
{
    public void Configure(EntityTypeBuilder<Widget> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BroadcasterId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(255);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Version).IsRequired().HasMaxLength(20).HasDefaultValue("1.0.0");
        builder.Property(e => e.Framework).IsRequired().HasMaxLength(20).HasDefaultValue("vanilla");
        builder.Property(e => e.TemplateId).HasMaxLength(100);

        // JSON columns
        builder.Property(e => e.EventSubscriptions).HasColumnType("jsonb");
        builder.Property(e => e.Settings).HasColumnType("jsonb");

        // Relationships
        builder.HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete filter
        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
