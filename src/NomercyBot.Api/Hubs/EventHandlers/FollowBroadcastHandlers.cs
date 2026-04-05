// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Api.Hubs.Dtos;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Api.Hubs.EventHandlers;

/// <summary>Broadcasts follow alerts to dashboard/overlay clients.</summary>
public sealed class FollowBroadcastHandler : IEventHandler<FollowEvent>
{
    private readonly IDashboardNotifier _notifier;

    public FollowBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(FollowEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.NotifyChannelAsync(
            @event.BroadcasterId,
            "follow",
            new FollowAlertDto(
                @event.UserId,
                @event.UserDisplayName,
                @event.UserLogin,
                @event.FollowedAt
            ),
            ct
        );
    }
}

/// <summary>Broadcasts new follower alerts (IRC fallback path) to dashboard/overlay clients.</summary>
public sealed class NewFollowerBroadcastHandler : IEventHandler<NewFollowerEvent>
{
    private readonly IDashboardNotifier _notifier;

    public NewFollowerBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(NewFollowerEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.NotifyChannelAsync(
            @event.BroadcasterId,
            "follow",
            new FollowAlertDto(@event.UserId, @event.UserDisplayName, @event.UserLogin, null),
            ct
        );
    }
}
