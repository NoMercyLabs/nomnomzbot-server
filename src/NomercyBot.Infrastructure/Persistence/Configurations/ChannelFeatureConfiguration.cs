using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class ChannelFeatureConfiguration : IEntityTypeConfiguration<ChannelFeature>
{
    public void Configure(EntityTypeBuilder<ChannelFeature> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BroadcasterId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.FeatureKey).IsRequired().HasMaxLength(50);

        // JSON columns
        builder.Property(e => e.RequiredScopes).HasColumnType("jsonb");

        // Relationships
        builder.HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => new { e.BroadcasterId, e.FeatureKey })
            .IsUnique()
            .HasDatabaseName("IX_ChannelFeature_BroadcasterId_FeatureKey");
    }
}
