// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Api.Hubs.Dtos;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Api.Hubs.EventHandlers;

/// <summary>
/// Broadcasts StreamStatusChanged to all dashboard clients when a stream goes online.
/// </summary>
public sealed class ChannelOnlineBroadcastHandler : IEventHandler<ChannelOnlineEvent>
{
    private readonly IDashboardNotifier _notifier;

    public ChannelOnlineBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(ChannelOnlineEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        StreamStatusDto dto = new(
            IsLive: true,
            StreamId: null,
            Title: @event.StreamTitle,
            GameName: @event.GameName,
            StartedAt: @event.StartedAt.ToString("O")
        );

        return _notifier.SendStreamStatusAsync(@event.BroadcasterId, dto, ct);
    }
}

/// <summary>
/// Broadcasts StreamStatusChanged to all dashboard clients when a stream goes offline.
/// </summary>
public sealed class ChannelOfflineBroadcastHandler : IEventHandler<ChannelOfflineEvent>
{
    private readonly IDashboardNotifier _notifier;

    public ChannelOfflineBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(ChannelOfflineEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        StreamStatusDto dto = new(
            IsLive: false,
            StreamId: null,
            Title: null,
            GameName: null,
            StartedAt: null
        );

        return _notifier.SendStreamStatusAsync(@event.BroadcasterId, dto, ct);
    }
}
