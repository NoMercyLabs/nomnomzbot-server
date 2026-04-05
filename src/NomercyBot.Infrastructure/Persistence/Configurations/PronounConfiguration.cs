// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class PronounConfiguration : IEntityTypeConfiguration<Pronoun>
{
    public void Configure(EntityTypeBuilder<Pronoun> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(50);

        builder.Property(e => e.Subject).IsRequired().HasMaxLength(20);

        builder.Property(e => e.Object).IsRequired().HasMaxLength(20);

        builder.Property(e => e.Singular).IsRequired();
    }
}
