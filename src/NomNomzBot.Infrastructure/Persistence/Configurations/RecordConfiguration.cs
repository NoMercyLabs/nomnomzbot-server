// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class RecordConfiguration : IEntityTypeConfiguration<Record>
{
    public void Configure(EntityTypeBuilder<Record> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BroadcasterId).IsRequired().HasMaxLength(50);

        builder.Property(e => e.RecordType).IsRequired().HasMaxLength(50);

        builder.Property(e => e.Data).IsRequired().HasColumnType("jsonb");

        builder.Property(e => e.UserId).IsRequired().HasMaxLength(50);

        builder
            .HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(e => new { e.BroadcasterId, e.RecordType })
            .HasDatabaseName("IX_Record_BroadcasterId_RecordType");

        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
