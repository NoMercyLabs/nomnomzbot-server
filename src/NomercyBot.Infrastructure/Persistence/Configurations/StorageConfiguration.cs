// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class StorageConfiguration : IEntityTypeConfiguration<Storage>
{
    public void Configure(EntityTypeBuilder<Storage> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BroadcasterId)
            .HasMaxLength(50);

        builder.Property(e => e.Key)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.SecureValue)
            .HasMaxLength(4096);

        builder.HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.Key, e.BroadcasterId })
            .IsUnique()
            .HasFilter("\"BroadcasterId\" IS NOT NULL")
            .HasDatabaseName("IX_Storage_Key_BroadcasterId");
    }
}
