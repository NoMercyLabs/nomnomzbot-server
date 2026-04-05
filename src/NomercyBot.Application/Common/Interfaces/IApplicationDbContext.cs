// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Channel> Channels { get; }
    DbSet<ChannelModerator> ChannelModerators { get; }
    DbSet<Service> Services { get; }
    DbSet<Command> Commands { get; }
    DbSet<Reward> Rewards { get; }
    DbSet<Widget> Widgets { get; }
    DbSet<EventSubscription> EventSubscriptions { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<ChannelEvent> ChannelEvents { get; }
    DbSet<NoMercyBot.Domain.Entities.Stream> Streams { get; }
    DbSet<NoMercyBot.Domain.Entities.Configuration> Configurations { get; }
    DbSet<Storage> Storages { get; }
    DbSet<Record> Records { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<ChannelFeature> ChannelFeatures { get; }
    DbSet<ChannelBotAuthorization> ChannelBotAuthorizations { get; }
    DbSet<DiscordServerAuthorization> DiscordServerAuthorizations { get; }
    DbSet<ChannelSubscription> ChannelSubscriptions { get; }
    DbSet<TtsVoice> TtsVoices { get; }
    DbSet<UserTtsVoice> UserTtsVoices { get; }
    DbSet<TtsUsageRecord> TtsUsageRecords { get; }
    DbSet<TtsCacheEntry> TtsCacheEntries { get; }
    DbSet<Pronoun> Pronouns { get; }
    DbSet<DeletionAuditLog> DeletionAuditLogs { get; }
    DbSet<NoMercyBot.Domain.Entities.Timer> Timers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
