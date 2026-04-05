// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Api.Hubs.Dtos;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Api.Hubs.EventHandlers;

/// <summary>Broadcasts channel point reward redemptions to dashboard clients.</summary>
public sealed class RewardRedeemedBroadcastHandler : IEventHandler<RewardRedeemedEvent>
{
    private readonly IDashboardNotifier _notifier;

    public RewardRedeemedBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(RewardRedeemedEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        RewardRedeemedDto dto = new(
            BroadcasterId: @event.BroadcasterId,
            RewardId: @event.RewardId,
            RewardTitle: @event.RewardTitle,
            RedemptionId: @event.RedemptionId,
            UserId: @event.UserId,
            UserDisplayName: @event.UserDisplayName,
            Cost: @event.Cost,
            UserInput: @event.UserInput,
            Timestamp: @event.Timestamp.ToString("O")
        );

        return _notifier.SendRewardRedeemedAsync(@event.BroadcasterId, dto, ct);
    }
}
