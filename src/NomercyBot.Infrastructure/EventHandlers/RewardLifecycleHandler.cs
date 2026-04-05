// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Domain.Entities;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.EventHandlers;

/// <summary>
/// Keeps local Reward records in sync with Twitch-side reward lifecycle events.
/// Creates/updates/removes local Reward entities when Twitch fires reward lifecycle events.
/// </summary>
public sealed class RewardLifecycleHandler
    : IEventHandler<RewardCreatedEvent>,
        IEventHandler<RewardUpdatedEvent>,
        IEventHandler<RewardRemovedEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RewardLifecycleHandler> _logger;

    public RewardLifecycleHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<RewardLifecycleHandler> logger
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(RewardCreatedEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var existing = await db.Rewards.FirstOrDefaultAsync(
            r =>
                r.BroadcasterId == @event.BroadcasterId
                && r.TwitchRewardId == @event.TwitchRewardId,
            ct
        );

        if (existing is not null)
            return; // already tracked

        db.Rewards.Add(
            new Reward
            {
                Id = Guid.NewGuid(),
                BroadcasterId = @event.BroadcasterId,
                TwitchRewardId = @event.TwitchRewardId,
                Title = @event.Title,
                Cost = @event.Cost,
                IsEnabled = @event.IsEnabled,
                IsPlatform = true,
            }
        );
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Reward created on Twitch: '{Title}' ({TwitchRewardId}) for {BroadcasterId}",
            @event.Title,
            @event.TwitchRewardId,
            @event.BroadcasterId
        );
    }

    public async Task HandleAsync(RewardUpdatedEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var reward = await db.Rewards.FirstOrDefaultAsync(
            r =>
                r.BroadcasterId == @event.BroadcasterId
                && r.TwitchRewardId == @event.TwitchRewardId,
            ct
        );

        if (reward is null)
            return;

        reward.Title = @event.Title;
        reward.Cost = @event.Cost;
        reward.IsEnabled = @event.IsEnabled;
        await db.SaveChangesAsync(ct);
    }

    public async Task HandleAsync(RewardRemovedEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var reward = await db.Rewards.FirstOrDefaultAsync(
            r =>
                r.BroadcasterId == @event.BroadcasterId
                && r.TwitchRewardId == @event.TwitchRewardId,
            ct
        );

        if (reward is null)
            return;

        // Soft-delete — keep config (PipelineJson) for potential re-creation
        reward.IsEnabled = false;
        reward.TwitchRewardId = null;
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Reward removed from Twitch: '{Title}' for {BroadcasterId}",
            @event.Title,
            @event.BroadcasterId
        );
    }
}
