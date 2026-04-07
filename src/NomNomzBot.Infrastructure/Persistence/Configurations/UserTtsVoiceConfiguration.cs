// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class UserTtsVoiceConfiguration : IEntityTypeConfiguration<UserTtsVoice>
{
    public void Configure(EntityTypeBuilder<UserTtsVoice> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BroadcasterId).IsRequired().HasMaxLength(50);

        builder.Property(e => e.UserId).IsRequired().HasMaxLength(50);

        builder.Property(e => e.VoiceId).IsRequired().HasMaxLength(255);

        builder
            .HasIndex(e => new { e.BroadcasterId, e.UserId })
            .IsUnique()
            .HasDatabaseName("IX_UserTtsVoice_BroadcasterId_UserId");
    }
}
