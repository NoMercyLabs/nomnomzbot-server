// SPDX-License-Identifier: AGPL-3.0-or-later

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

        builder.Property(e => e.IsEnabled).HasDefaultValue(false);

        builder
            .Property(e => e.RequiredScopes)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb");

        builder.HasIndex(e => new { e.BroadcasterId, e.FeatureKey }).IsUnique();

        builder
            .HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
