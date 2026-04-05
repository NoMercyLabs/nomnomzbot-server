// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BroadcasterId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.SubjectType)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.SubjectId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ResourceType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.ResourceId)
            .HasMaxLength(255);

        builder.Property(e => e.PermissionValue)
            .IsRequired()
            .HasMaxLength(5);

        builder.HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.BroadcasterId, e.SubjectType, e.SubjectId, e.ResourceType })
            .IsUnique()
            .HasDatabaseName("IX_Permission_BroadcasterId_Subject_ResourceType");

        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
