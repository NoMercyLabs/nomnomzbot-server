// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class ChannelBotAuthorizationConfiguration : IEntityTypeConfiguration<ChannelBotAuthorization>
{
    public void Configure(EntityTypeBuilder<ChannelBotAuthorization> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BroadcasterId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.AuthorizedBy)
            .HasMaxLength(50);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(e => e.BroadcasterId)
            .IsUnique();

        builder.HasOne(e => e.Channel)
            .WithOne()
            .HasForeignKey<ChannelBotAuthorization>(e => e.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
