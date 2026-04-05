// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Api.Hubs.Dtos;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Api.Hubs.EventHandlers;

/// <summary>Broadcasts cheer/bits alerts to dashboard/overlay clients.</summary>
public sealed class CheerBroadcastHandler : IEventHandler<CheerEvent>
{
    private readonly IDashboardNotifier _notifier;

    public CheerBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(CheerEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.NotifyChannelAsync(
            @event.BroadcasterId,
            "cheer",
            new CheerAlertDto(
                @event.IsAnonymous ? null : @event.UserId,
                @event.IsAnonymous ? "Anonymous" : @event.UserDisplayName,
                @event.Bits,
                @event.Message,
                @event.IsAnonymous
            ),
            ct
        );
    }
}
