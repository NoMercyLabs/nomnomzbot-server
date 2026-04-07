// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class ChannelSubscriptionConfiguration : IEntityTypeConfiguration<ChannelSubscription>
{
    public void Configure(EntityTypeBuilder<ChannelSubscription> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BroadcasterId).IsRequired().HasMaxLength(50);

        builder.Property(e => e.Tier).IsRequired().HasMaxLength(20).HasDefaultValue("free");

        builder.Property(e => e.StripeCustomerId).HasMaxLength(255);

        builder.Property(e => e.StripeSubscriptionId).HasMaxLength(255);

        builder.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("active");

        builder
            .HasIndex(e => e.BroadcasterId)
            .IsUnique()
            .HasDatabaseName("IX_ChannelSubscription_BroadcasterId");

        builder
            .HasOne(e => e.Channel)
            .WithOne()
            .HasForeignKey<ChannelSubscription>(e => e.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
