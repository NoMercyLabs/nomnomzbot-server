using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // Core
    public DbSet<User> Users => Set<User>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<ChannelModerator> ChannelModerators => Set<ChannelModerator>();
    public DbSet<Service> Services => Set<Service>();

    // Bot features
    public DbSet<Command> Commands => Set<Command>();
    public DbSet<Reward> Rewards => Set<Reward>();
    public DbSet<Widget> Widgets => Set<Widget>();
    public DbSet<EventSubscription> EventSubscriptions => Set<EventSubscription>();

    // Chat
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ChannelEvent> ChannelEvents => Set<ChannelEvent>();
    public DbSet<Domain.Entities.Stream> Streams => Set<Domain.Entities.Stream>();

    // Config & Storage
    public DbSet<NoMercyBot.Domain.Entities.Configuration> Configurations =>
        Set<NoMercyBot.Domain.Entities.Configuration>();
    public DbSet<Storage> Storages => Set<Storage>();
    public DbSet<Record> Records => Set<Record>();

    // Permissions
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<ChannelFeature> ChannelFeatures => Set<ChannelFeature>();

    // Auth & Billing
    public DbSet<ChannelBotAuthorization> ChannelBotAuthorizations =>
        Set<ChannelBotAuthorization>();
    public DbSet<DiscordServerAuthorization> DiscordServerAuthorizations =>
        Set<DiscordServerAuthorization>();
    public DbSet<ChannelSubscription> ChannelSubscriptions => Set<ChannelSubscription>();

    // TTS
    public DbSet<TtsVoice> TtsVoices => Set<TtsVoice>();
    public DbSet<UserTtsVoice> UserTtsVoices => Set<UserTtsVoice>();
    public DbSet<TtsUsageRecord> TtsUsageRecords => Set<TtsUsageRecord>();
    public DbSet<TtsCacheEntry> TtsCacheEntries => Set<TtsCacheEntry>();

    // Reference data
    public DbSet<Pronoun> Pronouns => Set<Pronoun>();

    // Audit
    public DbSet<DeletionAuditLog> DeletionAuditLogs => Set<DeletionAuditLog>();

    // Timers
    public DbSet<Domain.Entities.Timer> Timers => Set<Domain.Entities.Timer>();

    // Event responses
    public DbSet<EventResponse> EventResponses => Set<EventResponse>();

    // Watch streaks
    public DbSet<WatchStreak> WatchStreaks => Set<WatchStreak>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
