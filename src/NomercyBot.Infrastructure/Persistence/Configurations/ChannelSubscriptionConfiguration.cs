using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class ChannelSubscriptionConfiguration : IEntityTypeConfiguration<ChannelSubscription>
{
    public void Configure(EntityTypeBuilder<ChannelSubscription> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BroadcasterId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Tier).IsRequired().HasMaxLength(20).HasDefaultValue("free");
        builder.Property(e => e.StripeCustomerId).HasMaxLength(255);
        builder.Property(e => e.StripeSubscriptionId).HasMaxLength(255);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("active");

        // Relationships
        builder.HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.BroadcasterId)
            .IsUnique()
            .HasDatabaseName("IX_ChannelSubscription_BroadcasterId");
    }
}
