using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasMaxLength(255);
        builder.Property(e => e.BroadcasterId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.UserId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Username).IsRequired().HasMaxLength(255);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(255);
        builder.Property(e => e.UserType).IsRequired().HasMaxLength(20);
        builder.Property(e => e.ColorHex).HasMaxLength(7);
        builder.Property(e => e.Message).IsRequired();
        builder.Property(e => e.MessageType).IsRequired().HasMaxLength(50).HasDefaultValue("text");
        builder.Property(e => e.ReplyToMessageId).HasMaxLength(255);
        builder.Property(e => e.StreamId).HasMaxLength(50);

        // JSON columns
        builder.Property(e => e.Fragments).HasColumnType("jsonb");
        builder.Property(e => e.Badges).HasColumnType("jsonb");

        // Relationships
        builder.HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.BroadcasterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Stream)
            .WithMany()
            .HasForeignKey(e => e.StreamId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(e => e.BroadcasterId).HasDatabaseName("IX_ChatMessage_BroadcasterId");
        builder.HasIndex(e => new { e.BroadcasterId, e.CreatedAt }).HasDatabaseName("IX_ChatMessage_BroadcasterId_CreatedAt");
        builder.HasIndex(e => e.UserId).HasDatabaseName("IX_ChatMessage_UserId");

        // Soft delete filter
        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
