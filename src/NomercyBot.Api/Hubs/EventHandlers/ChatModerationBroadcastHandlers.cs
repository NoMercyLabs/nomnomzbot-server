// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Api.Hubs.Dtos;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Api.Hubs.EventHandlers;

/// <summary>Broadcasts chat cleared events to dashboard clients.</summary>
public sealed class ChatClearedBroadcastHandler : IEventHandler<ChatClearedEvent>
{
    private readonly IDashboardNotifier _notifier;

    public ChatClearedBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(ChatClearedEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.NotifyChannelAsync(
            @event.BroadcasterId,
            "chat_cleared",
            new ChatClearedDto(@event.ClearedByUserId),
            ct
        );
    }
}

/// <summary>Broadcasts message deleted events to dashboard/overlay clients.</summary>
public sealed class ChatMessageDeletedBroadcastHandler : IEventHandler<ChatMessageDeletedEvent>
{
    private readonly IDashboardNotifier _notifier;

    public ChatMessageDeletedBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(ChatMessageDeletedEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.NotifyChannelAsync(
            @event.BroadcasterId,
            "message_deleted",
            new MessageDeletedDto(@event.MessageId, @event.DeletedByUserId, @event.TargetUserId),
            ct
        );
    }
}
