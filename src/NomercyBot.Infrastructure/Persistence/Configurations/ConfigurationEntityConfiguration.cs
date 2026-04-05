using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class ConfigurationEntityConfiguration : IEntityTypeConfiguration<NoMercyBot.Domain.Entities.Configuration>
{
    public void Configure(EntityTypeBuilder<NoMercyBot.Domain.Entities.Configuration> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BroadcasterId).HasMaxLength(50);
        builder.Property(e => e.Key).IsRequired().HasMaxLength(255);
        builder.Property(e => e.SecureValue).HasMaxLength(4096);

        // Relationships
        builder.HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => new { e.Key, e.BroadcasterId })
            .IsUnique()
            .HasDatabaseName("IX_Configuration_Key_BroadcasterId");
    }
}
