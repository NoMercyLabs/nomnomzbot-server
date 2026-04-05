// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class ChannelEventConfiguration : IEntityTypeConfiguration<ChannelEvent>
{
    public void Configure(EntityTypeBuilder<ChannelEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired().HasMaxLength(50);

        builder.Property(e => e.ChannelId).HasMaxLength(50);

        builder.Property(e => e.UserId).HasMaxLength(50);

        builder.Property(e => e.Type).IsRequired().HasMaxLength(50);

        builder.Property(e => e.Data).HasColumnType("jsonb");

        builder
            .HasOne(e => e.Channel)
            .WithMany(c => c.Events)
            .HasForeignKey(e => e.ChannelId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasIndex(e => new { e.ChannelId, e.Type })
            .HasDatabaseName("IX_ChannelEvent_ChannelId_Type");

        builder
            .HasIndex(e => new { e.ChannelId, e.CreatedAt })
            .HasDatabaseName("IX_ChannelEvent_ChannelId_CreatedAt");
    }
}
