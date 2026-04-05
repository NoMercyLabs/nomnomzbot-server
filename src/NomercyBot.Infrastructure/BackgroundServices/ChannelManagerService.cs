// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Contracts.Twitch;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.BackgroundServices;

/// <summary>
/// Starts up the bot by joining all enabled, onboarded channels.
/// For each channel:
///   1. Joins IRC (fallback / watch streaks)
///   2. Subscribes to EventSub events (primary chat + all Twitch events)
///   3. Registers the channel in IChannelRegistry (loads commands into memory)
/// </summary>
public class ChannelManagerService : BackgroundService
{
    // Core EventSub subscriptions every channel needs
    private static readonly string[] RequiredSubscriptions =
    [
        "channel.chat.message",
        "channel.chat.message_delete",
        "channel.follow",
        "channel.subscribe",
        "channel.subscription.gift",
        "channel.cheer",
        "channel.raid",
        "channel.ban",
        "channel.channel_points_custom_reward_redemption.add",
        "channel.channel_points_custom_reward.add",
        "channel.channel_points_custom_reward.update",
        "channel.channel_points_custom_reward.remove",
        "channel.poll.begin",
        "channel.poll.end",
        "channel.prediction.begin",
        "channel.prediction.lock",
        "channel.prediction.end",
        "channel.hype_train.begin",
        "channel.hype_train.end",
        "stream.online",
        "stream.offline",
        "channel.update",
        "channel.shoutout.create",
        "channel.shoutout.receive",
    ];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChannelManagerService> _logger;

    public ChannelManagerService(
        IServiceProvider serviceProvider,
        ILogger<ChannelManagerService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ChannelManagerService starting — joining enabled channels.");

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var chatService = scope.ServiceProvider.GetRequiredService<ITwitchChatService>();
        var eventSubService = scope.ServiceProvider.GetRequiredService<ITwitchEventSubService>();
        var registry = scope.ServiceProvider.GetRequiredService<IChannelRegistry>();

        var channels = await db
            .Channels.Where(c => c.Enabled && c.IsOnboarded)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(stoppingToken);

        _logger.LogInformation("Found {Count} enabled channels to join", channels.Count);

        foreach (var channel in channels)
        {
            try
            {
                // 1. Join IRC (fallback for features not in EventSub)
                await chatService.JoinChannelAsync(channel.Name, stoppingToken);

                // 2. Subscribe to all required EventSub events
                foreach (var eventType in RequiredSubscriptions)
                {
                    try
                    {
                        await eventSubService.SubscribeAsync(channel.Id, eventType, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to subscribe {EventType} for channel {ChannelName}",
                            eventType,
                            channel.Name
                        );
                    }
                }

                // 3. Register channel in registry (loads commands into memory)
                await registry.GetOrCreateAsync(channel.Id, channel.Name, stoppingToken);

                _logger.LogInformation(
                    "Channel #{ChannelName} ({ChannelId}) joined and subscribed",
                    channel.Name,
                    channel.Id
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize channel #{ChannelName}", channel.Name);
            }
        }

        _logger.LogInformation(
            "ChannelManagerService startup complete — {Count} channels active",
            channels.Count
        );
    }
}
