// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Api.Hubs.Dtos;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Api.Hubs.EventHandlers;

/// <summary>Broadcasts user ban events to dashboard clients.</summary>
public sealed class UserBannedBroadcastHandler : IEventHandler<UserBannedEvent>
{
    private readonly IDashboardNotifier _notifier;

    public UserBannedBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(UserBannedEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.SendModActionAsync(
            @event.BroadcasterId,
            new(
                "ban",
                @event.ModeratorUserId,
                @event.TargetUserId,
                @event.Reason,
                null
            ),
            ct
        );
    }
}

/// <summary>Broadcasts user timeout events to dashboard clients.</summary>
public sealed class UserTimedOutBroadcastHandler : IEventHandler<UserTimedOutEvent>
{
    private readonly IDashboardNotifier _notifier;

    public UserTimedOutBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(UserTimedOutEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.SendModActionAsync(
            @event.BroadcasterId,
            new(
                "timeout",
                @event.ModeratorUserId,
                @event.TargetUserId,
                @event.Reason,
                @event.DurationSeconds
            ),
            ct
        );
    }
}

/// <summary>Broadcasts user unban events to dashboard clients.</summary>
public sealed class UserUnbannedBroadcastHandler : IEventHandler<UserUnbannedEvent>
{
    private readonly IDashboardNotifier _notifier;

    public UserUnbannedBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(UserUnbannedEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.SendModActionAsync(
            @event.BroadcasterId,
            new("unban", @event.ModeratorUserId, @event.TargetUserId, null, null),
            ct
        );
    }
}
