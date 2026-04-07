// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class EventSubscriptionConfiguration : IEntityTypeConfiguration<EventSubscription>
{
    public void Configure(EntityTypeBuilder<EventSubscription> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired().HasMaxLength(50);

        builder.Property(e => e.BroadcasterId).IsRequired().HasMaxLength(50);

        builder.Property(e => e.Provider).IsRequired().HasMaxLength(50);

        builder.Property(e => e.EventType).IsRequired().HasMaxLength(100);

        builder.Property(e => e.Description).HasMaxLength(500);

        builder.Property(e => e.Enabled).HasDefaultValue(true);

        builder.Property(e => e.Version).HasMaxLength(50);

        builder.Property(e => e.SubscriptionId).HasMaxLength(255);

        builder.Property(e => e.SessionId).HasMaxLength(255);

        builder.Property(e => e.Metadata).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");

        builder.Property(e => e.Condition).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");

        builder
            .HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
