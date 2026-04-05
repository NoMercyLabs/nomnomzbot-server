using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasMaxLength(50);
        builder.Property(e => e.Username).IsRequired().HasMaxLength(255);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(255);
        builder.Property(e => e.NickName).HasMaxLength(255);
        builder.Property(e => e.Timezone).HasMaxLength(50);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.ProfileImageUrl).HasMaxLength(2048);
        builder.Property(e => e.OfflineImageUrl).HasMaxLength(2048);
        builder.Property(e => e.Color).HasMaxLength(7);
        builder.Property(e => e.BroadcasterType).IsRequired().HasMaxLength(50).HasDefaultValue("");

        // Pronoun navigation (optional)
        builder.HasOne(e => e.Pronoun)
            .WithMany()
            .HasForeignKey("PronounId")
            .OnDelete(DeleteBehavior.SetNull);

        // Navigation: one-to-one with Channel (optional)
        builder.HasOne(e => e.Channel)
            .WithOne(c => c.User)
            .HasForeignKey<Channel>(c => c.Id);

        builder.HasIndex(e => e.Username);
    }
}
