// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Api.Hubs.Dtos;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Api.Hubs.EventHandlers;

/// <summary>Broadcasts music playback state changes to dashboard clients.</summary>
public sealed class PlaybackStateBroadcastHandler : IEventHandler<PlaybackStateChangedEvent>
{
    private readonly IDashboardNotifier _notifier;

    public PlaybackStateBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(PlaybackStateChangedEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        MusicTrackDto? track = @event.TrackName is not null
            ? new MusicTrackDto(@event.TrackName, string.Empty, string.Empty, null, 0, "unknown")
            : null;

        return _notifier.SendMusicStateAsync(
            @event.BroadcasterId,
            new(@event.IsPlaying, track),
            ct
        );
    }
}
