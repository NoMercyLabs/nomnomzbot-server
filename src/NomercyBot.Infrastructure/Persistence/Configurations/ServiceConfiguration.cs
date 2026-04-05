using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(50);
        builder.Property(e => e.BroadcasterId).HasMaxLength(50);
        builder.Property(e => e.ClientId).HasMaxLength(512);
        builder.Property(e => e.ClientSecret).HasMaxLength(512);
        builder.Property(e => e.UserName).HasMaxLength(255);
        builder.Property(e => e.UserId).HasMaxLength(50);
        builder.Property(e => e.AccessToken).HasMaxLength(2048);
        builder.Property(e => e.RefreshToken).HasMaxLength(2048);

        // JSON columns
        builder.Property(e => e.Scopes).HasColumnType("jsonb");

        // Relationships
        builder.HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.BroadcasterId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(e => new { e.Name, e.BroadcasterId })
            .IsUnique()
            .HasDatabaseName("IX_Service_Name_BroadcasterId");
    }
}
