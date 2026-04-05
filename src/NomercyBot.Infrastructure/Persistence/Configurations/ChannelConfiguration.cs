using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasMaxLength(50);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(25);
        builder.Property(e => e.ShoutoutTemplate).HasMaxLength(450);
        builder.Property(e => e.UsernamePronunciation).HasMaxLength(100);
        builder.Property(e => e.OverlayToken).IsRequired().HasMaxLength(36);
        builder.Property(e => e.Language).HasMaxLength(50);
        builder.Property(e => e.GameId).HasMaxLength(50);
        builder.Property(e => e.GameName).HasMaxLength(255);
        builder.Property(e => e.Title).HasMaxLength(255);

        // JSON columns
        builder.Property(e => e.Tags).HasColumnType("jsonb");
        builder.Property(e => e.ContentLabels).HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(e => e.Enabled).HasDatabaseName("IX_Channel_Enabled");
        builder.HasIndex(e => e.OverlayToken).IsUnique().HasDatabaseName("IX_Channel_OverlayToken");

        // Soft delete filter
        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
