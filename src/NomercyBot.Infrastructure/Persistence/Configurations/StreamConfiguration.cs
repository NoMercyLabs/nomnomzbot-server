using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class StreamConfiguration : IEntityTypeConfiguration<Domain.Entities.Stream>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Stream> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasMaxLength(50);
        builder.Property(e => e.ChannelId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Language).HasMaxLength(50);
        builder.Property(e => e.GameId).HasMaxLength(50);
        builder.Property(e => e.GameName).HasMaxLength(255);
        builder.Property(e => e.Title).HasMaxLength(255);

        // JSON columns
        builder.Property(e => e.Tags).HasColumnType("jsonb");
        builder.Property(e => e.ContentLabels).HasColumnType("jsonb");

        // Relationships
        builder.HasOne(e => e.Channel)
            .WithMany(c => c.Streams)
            .HasForeignKey(e => e.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
