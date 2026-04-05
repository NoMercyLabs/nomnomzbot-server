using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class ChannelBotAuthorizationConfiguration : IEntityTypeConfiguration<ChannelBotAuthorization>
{
    public void Configure(EntityTypeBuilder<ChannelBotAuthorization> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BroadcasterId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.AuthorizedBy).HasMaxLength(50);

        // Relationships
        builder.HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.BroadcasterId)
            .IsUnique()
            .HasDatabaseName("IX_ChannelBotAuthorization_BroadcasterId");
    }
}
