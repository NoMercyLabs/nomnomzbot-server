using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class TtsUsageRecordConfiguration : IEntityTypeConfiguration<TtsUsageRecord>
{
    public void Configure(EntityTypeBuilder<TtsUsageRecord> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BroadcasterId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.UserId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Provider).IsRequired().HasMaxLength(50);
        builder.Property(e => e.VoiceId).IsRequired().HasMaxLength(255);

        // Indexes
        builder.HasIndex(e => new { e.BroadcasterId, e.CreatedAt });
        builder.HasIndex(e => e.UserId);
    }
}
