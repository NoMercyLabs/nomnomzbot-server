// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Contracts.Twitch;

namespace NoMercyBot.Infrastructure.BackgroundServices;

/// <summary>
/// Manages the bot's chat connections per channel.
/// On startup: joins all enabled, onboarded channels via IRC and subscribes
/// to EventSub events for each channel.
/// Monitors for channels that are enabled/disabled at runtime (polls every 5 minutes).
/// </summary>
public sealed class BotLifecycleService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BotLifecycleService> _logger;

    // Channel state tracked locally to detect joins/leaves
    private readonly HashSet<string> _joinedChannels = [];
    private readonly Lock _channelLock = new();

    // EventSub event types to subscribe to per channel
    private static readonly string[] ChannelEventTypes =
    [
        "channel.follow",
        "channel.subscribe",
        "channel.subscription.gift",
        "channel.cheer",
        "channel.raid",
        "channel.ban",
        "channel.channel_points_custom_reward_redemption.add",
        "channel.chat.message",
        "stream.online",
        "stream.offline",
    ];

    public BotLifecycleService(
        IServiceProvider serviceProvider,
        ILogger<BotLifecycleService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BotLifecycleService starting.");

        // Initial join on startup
        await SyncChannelsAsync(stoppingToken);

        // Periodic sync every 5 minutes to detect dynamic channel changes
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SyncChannelsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "BotLifecycleService: Error syncing channels");
            }
        }
    }

    private async Task SyncChannelsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var chatService = scope.ServiceProvider.GetRequiredService<ITwitchChatService>();
        var eventSub = scope.ServiceProvider.GetRequiredService<ITwitchEventSubService>();

        // Get all currently enabled channels
        var activeChannels = await db.Channels
            .Where(c => c.Enabled && c.IsOnboarded)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(ct);

        var activeIds = activeChannels.Select(c => c.Id).ToHashSet();

        HashSet<string> toJoin;
        HashSet<string> toLeave;

        lock (_channelLock)
        {
            toJoin = activeIds.Except(_joinedChannels).ToHashSet();
            toLeave = _joinedChannels.Except(activeIds).ToHashSet();
        }

        // Join new channels
        foreach (var channel in activeChannels.Where(c => toJoin.Contains(c.Id)))
        {
            try
            {
                await chatService.JoinChannelAsync(channel.Name, ct);

                // Subscribe to EventSub events for this channel
                foreach (var eventType in ChannelEventTypes)
                {
                    await eventSub.SubscribeAsync(channel.Id, eventType, ct);
                }

                lock (_channelLock) _joinedChannels.Add(channel.Id);
                _logger.LogInformation("BotLifecycleService: Joined channel #{ChannelName} ({Id})", channel.Name, channel.Id);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "BotLifecycleService: Failed to join channel #{ChannelName}", channel.Name);
            }
        }

        // Leave channels that are no longer active
        foreach (var channelId in toLeave)
        {
            try
            {
                var channel = await db.Channels.FindAsync([channelId], ct);
                if (channel is not null)
                    await chatService.LeaveChannelAsync(channel.Name, ct);

                lock (_channelLock) _joinedChannels.Remove(channelId);
                _logger.LogInformation("BotLifecycleService: Left channel {ChannelId}", channelId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "BotLifecycleService: Failed to leave channel {ChannelId}", channelId);
            }
        }
    }
}
