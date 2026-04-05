// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Api.Hubs.Dtos;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Api.Hubs.EventHandlers;

/// <summary>Broadcasts incoming raid alerts to dashboard/overlay clients.</summary>
public sealed class RaidBroadcastHandler : IEventHandler<RaidEvent>
{
    private readonly IDashboardNotifier _notifier;

    public RaidBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(RaidEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.NotifyChannelAsync(
            @event.BroadcasterId,
            "raid",
            new RaidAlertDto(
                @event.FromUserId,
                @event.FromDisplayName,
                @event.FromLogin,
                @event.ViewerCount
            ),
            ct
        );
    }
}
