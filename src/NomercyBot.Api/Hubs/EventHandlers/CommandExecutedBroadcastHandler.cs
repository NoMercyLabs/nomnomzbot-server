// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Api.Hubs.Dtos;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Api.Hubs.EventHandlers;

/// <summary>Broadcasts command execution results to dashboard clients.</summary>
public sealed class CommandExecutedBroadcastHandler : IEventHandler<AfterCommandExecutedEvent>
{
    private readonly IDashboardNotifier _notifier;
    private readonly IChannelRegistry _registry;

    public CommandExecutedBroadcastHandler(IDashboardNotifier notifier, IChannelRegistry registry)
    {
        _notifier = notifier;
        _registry = registry;
    }

    public Task HandleAsync(AfterCommandExecutedEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        if (@event.Succeeded)
        {
            ChannelContext? ctx = _registry.Get(@event.BroadcasterId);
            if (ctx is not null)
            {
                lock (ctx.Lock)
                    ctx.CommandsUsed++;
            }
        }

        return _notifier.SendCommandExecutedAsync(
            @event.BroadcasterId,
            new(
                @event.BroadcasterId,
                @event.CommandName,
                @event.TriggeredByUserId,
                @event.Succeeded,
                @event.Timestamp.ToString("O")
            ),
            ct
        );
    }
}
