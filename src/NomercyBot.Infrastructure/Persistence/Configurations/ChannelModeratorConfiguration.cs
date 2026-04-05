using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class ChannelModeratorConfiguration : IEntityTypeConfiguration<ChannelModerator>
{
    public void Configure(EntityTypeBuilder<ChannelModerator> builder)
    {
        builder.HasKey(e => new { e.ChannelId, e.UserId });

        builder.Property(e => e.ChannelId).HasMaxLength(50);
        builder.Property(e => e.UserId).HasMaxLength(50);
        builder.Property(e => e.Role).IsRequired().HasMaxLength(20).HasDefaultValue("moderator");
        builder.Property(e => e.GrantedBy).HasMaxLength(50);

        // Relationships
        builder.HasOne(e => e.Channel)
            .WithMany(c => c.Moderators)
            .HasForeignKey(e => e.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.UserId).HasDatabaseName("IX_ChannelModerator_UserId");
        builder.HasIndex(e => e.ChannelId).HasDatabaseName("IX_ChannelModerator_ChannelId");

        // Soft delete filter
        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
