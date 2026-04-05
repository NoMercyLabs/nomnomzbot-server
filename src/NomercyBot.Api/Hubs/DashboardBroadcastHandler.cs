// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Api.Hubs.Dtos;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Api.Hubs;

/// <summary>
/// Event handler that broadcasts domain events to connected dashboard clients via SignalR.
///
/// Subscribed events:
///   - ChatMessageReceivedEvent → IDashboardClient.ChatMessage
///
/// All handlers run on the event bus thread pool (parallel, failure-isolated).
/// </summary>
public sealed class DashboardBroadcastHandler : IEventHandler<ChatMessageReceivedEvent>
{
    private readonly IDashboardNotifier _notifier;

    public DashboardBroadcastHandler(IDashboardNotifier notifier)
    {
        _notifier = notifier;
    }

    public async Task HandleAsync(ChatMessageReceivedEvent @event, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId)) return;

        var dto = new DashboardChatMessageDto(
            MessageId: @event.MessageId,
            UserId: @event.UserId,
            UserDisplayName: @event.UserDisplayName,
            UserLogin: @event.UserLogin,
            Message: @event.Message,
            IsSubscriber: @event.IsSubscriber,
            IsVip: @event.IsVip,
            IsModerator: @event.IsModerator,
            Badges: @event.Badges.Select(b => $"{b.Key}/{b.Value}").ToArray(),
            Bits: @event.Bits,
            Color: null,
            Timestamp: @event.Timestamp.ToString("O"));

        await _notifier.SendChatMessageAsync(@event.BroadcasterId, dto, cancellationToken);
    }
}
