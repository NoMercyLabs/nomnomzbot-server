// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Api.Hubs.Dtos;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Api.Hubs.EventHandlers;

/// <summary>Broadcasts permission changes to dashboard clients.</summary>
public sealed class PermissionChangedBroadcastHandler : IEventHandler<PermissionChangedEvent>
{
    private readonly IDashboardNotifier _notifier;

    public PermissionChangedBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(PermissionChangedEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.SendPermissionChangedAsync(
            @event.BroadcasterId,
            new PermissionChangedDto(
                @event.SubjectType,
                @event.SubjectId,
                @event.ResourceType,
                @event.ResourceId,
                @event.NewPermissionValue
            ),
            ct
        );
    }
}
