using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class DeletionAuditLogConfiguration : IEntityTypeConfiguration<DeletionAuditLog>
{
    public void Configure(EntityTypeBuilder<DeletionAuditLog> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RequestType).IsRequired().HasMaxLength(30);
        builder.Property(e => e.SubjectIdHash).IsRequired().HasMaxLength(64);
        builder.Property(e => e.RequestedBy).IsRequired().HasMaxLength(20);

        // JSON column
        builder.Property(e => e.TablesAffected).HasColumnType("jsonb");

        // Immutable: no update operations expected
        builder.HasIndex(e => e.CompletedAt);
    }
}
