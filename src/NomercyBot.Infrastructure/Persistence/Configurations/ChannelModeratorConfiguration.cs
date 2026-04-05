// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class ChannelModeratorConfiguration : IEntityTypeConfiguration<ChannelModerator>
{
    public void Configure(EntityTypeBuilder<ChannelModerator> builder)
    {
        builder.HasKey(e => new { e.ChannelId, e.UserId });

        builder.Property(e => e.ChannelId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.UserId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Role)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("moderator");

        builder.Property(e => e.GrantedAt)
            .IsRequired();

        builder.Property(e => e.GrantedBy)
            .HasMaxLength(50);

        builder.HasOne(e => e.Channel)
            .WithMany(c => c.Moderators)
            .HasForeignKey(e => e.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
