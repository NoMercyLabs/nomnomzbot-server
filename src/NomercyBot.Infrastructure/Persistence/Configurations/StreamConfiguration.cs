// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class StreamConfiguration : IEntityTypeConfiguration<NoMercyBot.Domain.Entities.Stream>
{
    public void Configure(EntityTypeBuilder<NoMercyBot.Domain.Entities.Stream> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired().HasMaxLength(50);

        builder.Property(e => e.ChannelId).IsRequired().HasMaxLength(50);

        builder.Property(e => e.Language).HasMaxLength(50);

        builder.Property(e => e.GameId).HasMaxLength(50);

        builder.Property(e => e.GameName).HasMaxLength(255);

        builder.Property(e => e.Title).HasMaxLength(255);

        builder.Property(e => e.Delay).IsRequired();

        builder.Property(e => e.Tags).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");

        builder
            .Property(e => e.ContentLabels)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb");

        builder.Property(e => e.IsBrandedContent).IsRequired();

        builder
            .HasOne(e => e.Channel)
            .WithMany(c => c.Streams)
            .HasForeignKey(e => e.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
