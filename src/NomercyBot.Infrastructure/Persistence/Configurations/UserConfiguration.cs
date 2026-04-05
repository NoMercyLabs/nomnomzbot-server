// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired().HasMaxLength(50);

        builder.Property(e => e.Username).IsRequired().HasMaxLength(255);

        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(255);

        builder.Property(e => e.NickName).HasMaxLength(255);

        builder.Property(e => e.Timezone).HasMaxLength(50);

        builder.Property(e => e.Description).HasMaxLength(500);

        builder.Property(e => e.ProfileImageUrl).HasMaxLength(2048);

        builder.Property(e => e.OfflineImageUrl).HasMaxLength(2048);

        builder.Property(e => e.Color).HasMaxLength(7);

        builder.Property(e => e.BroadcasterType).IsRequired().HasMaxLength(50).HasDefaultValue("");

        builder.Property(e => e.Enabled).HasDefaultValue(true);

        builder.Property(e => e.PronounManualOverride).IsRequired();

        // Shadow FK for Pronoun navigation
        builder.Property<int?>("PronounId");

        builder
            .HasOne(e => e.Pronoun)
            .WithMany()
            .HasForeignKey("PronounId")
            .OnDelete(DeleteBehavior.SetNull);

        // Inverse of Channel.User (one-to-one defined on Channel side)
        builder
            .HasOne(e => e.Channel)
            .WithOne(c => c.User)
            .HasForeignKey<Channel>(c => c.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
