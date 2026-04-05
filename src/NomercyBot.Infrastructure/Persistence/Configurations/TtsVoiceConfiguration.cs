// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class TtsVoiceConfiguration : IEntityTypeConfiguration<TtsVoice>
{
    public void Configure(EntityTypeBuilder<TtsVoice> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.DisplayName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Locale)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.Gender)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.IsDefault)
            .HasDefaultValue(false);
    }
}
