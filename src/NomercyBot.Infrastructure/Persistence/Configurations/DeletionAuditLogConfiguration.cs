// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class DeletionAuditLogConfiguration : IEntityTypeConfiguration<DeletionAuditLog>
{
    public void Configure(EntityTypeBuilder<DeletionAuditLog> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RequestType)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(e => e.SubjectIdHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.RequestedBy)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.TablesAffected)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb");

        builder.Property(e => e.RowsDeleted)
            .IsRequired();

        builder.Property(e => e.CompletedAt)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();
    }
}
