// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that manages per-channel message timers.
///
/// Each timer has:
///   - IntervalMinutes: how often to fire
///   - MinChatActivity: minimum new chat messages since last fire before the timer will fire again
///   - Messages: round-robin list of messages to send
///
/// The service polls every minute, checking whether any timer is due.
/// Timer state (last fired, message index) is persisted back to the database.
/// </summary>
public sealed class TimerService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChannelRegistry _registry;
    private readonly ITemplateResolver _templateResolver;
    private readonly ILogger<TimerService> _logger;

    // Per-timer in-memory state: tracks message count at last fire for activity gating
    // Key: timer.Id, Value: MessageCount snapshot when the timer last fired
    private readonly ConcurrentDictionary<int, long> _messageCountAtLastFire = new();

    public TimerService(
        IServiceScopeFactory scopeFactory,
        IChannelRegistry registry,
        ITemplateResolver templateResolver,
        ILogger<TimerService> logger)
    {
        _scopeFactory = scopeFactory;
        _registry = registry;
        _templateResolver = templateResolver;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TimerService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "TimerService tick failed");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        // Only process channels the bot is actively connected to
        var liveChannels = _registry.GetAll();
        if (liveChannels.Count == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var chat = scope.ServiceProvider.GetRequiredService<IChatProvider>();

        var now = DateTime.UtcNow;
        var broadcasterIds = liveChannels.Select(c => c.BroadcasterId).ToList();

        var timers = await db.Timers
            .Where(t => t.IsEnabled
                && t.DeletedAt == null
                && broadcasterIds.Contains(t.BroadcasterId))
            .ToListAsync(ct);

        foreach (var timer in timers)
        {
            try
            {
                await ProcessTimerAsync(db, chat, timer, now, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Timer {TimerId} ({Name}) failed", timer.Id, timer.Name);
            }
        }
    }

    private async Task ProcessTimerAsync(
        IApplicationDbContext db,
        IChatProvider chat,
        Domain.Entities.Timer timer,
        DateTime now,
        CancellationToken ct)
    {
        // Check interval
        var nextFire = (timer.LastFiredAt ?? DateTime.MinValue).AddMinutes(timer.IntervalMinutes);
        if (now < nextFire) return;

        var channelCtx = _registry.Get(timer.BroadcasterId);
        if (channelCtx is null) return;

        // Check minimum chat activity since last fire
        if (timer.MinChatActivity > 0)
        {
            var countAtLastFire = _messageCountAtLastFire.GetValueOrDefault(timer.Id, 0L);
            var messagesSinceLastFire = channelCtx.MessageCount - countAtLastFire;
            if (messagesSinceLastFire < timer.MinChatActivity)
            {
                _logger.LogDebug(
                    "Timer {Name} skipped — only {Count}/{Required} messages since last fire",
                    timer.Name, messagesSinceLastFire, timer.MinChatActivity);
                return;
            }
        }

        if (timer.Messages.Count == 0) return;

        // Pick next message (round-robin)
        var messageTemplate = timer.Messages[timer.NextMessageIndex % timer.Messages.Count];

        // Resolve template variables
        var message = await _templateResolver.ResolveAsync(
            messageTemplate,
            new Dictionary<string, string>(),
            timer.BroadcasterId,
            ct);

        if (string.IsNullOrWhiteSpace(message)) return;

        // Send to chat
        await chat.SendMessageAsync(timer.BroadcasterId, message, cancellationToken: ct);

        // Persist state: LastFiredAt + NextMessageIndex
        timer.LastFiredAt = now;
        timer.NextMessageIndex = (timer.NextMessageIndex + 1) % timer.Messages.Count;
        await db.SaveChangesAsync(ct);

        // Record message count snapshot for next activity check
        _messageCountAtLastFire[timer.Id] = channelCtx.MessageCount;

        _logger.LogInformation(
            "Timer {Name} fired in channel {BroadcasterId}: \"{Message}\"",
            timer.Name, timer.BroadcasterId, message);
    }
}
