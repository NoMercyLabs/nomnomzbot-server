using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class DiscordServerAuthorizationConfiguration : IEntityTypeConfiguration<DiscordServerAuthorization>
{
    public void Configure(EntityTypeBuilder<DiscordServerAuthorization> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BroadcasterId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.GuildId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.GuildName).IsRequired().HasMaxLength(255);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
        builder.Property(e => e.ApprovedBy).HasMaxLength(50);

        // Relationships
        builder.HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => new { e.BroadcasterId, e.GuildId })
            .IsUnique()
            .HasDatabaseName("IX_DiscordServerAuth_BroadcasterId_GuildId");
    }
}
