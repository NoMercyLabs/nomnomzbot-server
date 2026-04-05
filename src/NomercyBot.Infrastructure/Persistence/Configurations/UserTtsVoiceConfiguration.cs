using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class UserTtsVoiceConfiguration : IEntityTypeConfiguration<UserTtsVoice>
{
    public void Configure(EntityTypeBuilder<UserTtsVoice> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BroadcasterId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.UserId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.VoiceId).IsRequired().HasMaxLength(255);

        // Indexes
        builder.HasIndex(e => new { e.UserId, e.BroadcasterId })
            .IsUnique()
            .HasDatabaseName("IX_UserTtsVoice_UserId_BroadcasterId");
    }
}
