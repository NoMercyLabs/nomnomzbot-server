using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class TtsCacheEntryConfiguration : IEntityTypeConfiguration<TtsCacheEntry>
{
    public void Configure(EntityTypeBuilder<TtsCacheEntry> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ContentHash).IsRequired().HasMaxLength(64);
        builder.Property(e => e.AudioData).IsRequired();
        builder.Property(e => e.Provider).IsRequired().HasMaxLength(50);
        builder.Property(e => e.VoiceId).IsRequired().HasMaxLength(255);

        // Indexes
        builder.HasIndex(e => e.ContentHash)
            .IsUnique()
            .HasDatabaseName("IX_TtsCacheEntry_ContentHash");
    }
}
