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
    public CommandExecutedBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(AfterCommandExecutedEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId)) return Task.CompletedTask;

        var dto = new CommandExecutedDto(
            CommandName: @event.CommandName,
            TriggeredByUserId: @event.TriggeredByUserId,
            Succeeded: @event.Succeeded,
            Timestamp: @event.Timestamp.ToString("O"));

        return _notifier.SendCommandExecutedAsync(@event.BroadcasterId, dto, ct);
    }
}

/// <summary>Broadcasts permission changes to dashboard clients.</summary>
public sealed class PermissionChangedBroadcastHandler : IEventHandler<PermissionChangedEvent>
{
    private readonly IDashboardNotifier _notifier;
    public PermissionChangedBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(PermissionChangedEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId)) return Task.CompletedTask;

        var dto = new PermissionChangedDto(
            SubjectType: @event.SubjectType,
            SubjectId: @event.SubjectId,
            ResourceType: @event.ResourceType,
            ResourceId: @event.ResourceId,
            Value: @event.NewPermissionValue);

        return _notifier.SendPermissionChangedAsync(@event.BroadcasterId, dto, ct);
    }
}

/// <summary>Broadcasts music playback state changes to dashboard clients.</summary>
public sealed class PlaybackStateBroadcastHandler : IEventHandler<PlaybackStateChangedEvent>
{
    private readonly IDashboardNotifier _notifier;
    public PlaybackStateBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(PlaybackStateChangedEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId)) return Task.CompletedTask;

        var dto = new MusicStateDto(
            IsPlaying: @event.IsPlaying,
            CurrentTrack: @event.TrackName is not null
                ? new MusicTrackDto(@event.TrackName, string.Empty, string.Empty, null, 0, "unknown")
                : null);

        return _notifier.SendMusicStateAsync(@event.BroadcasterId, dto, ct);
    }
}

/// <summary>Broadcasts integration connection events (Spotify, Discord, OBS) as dashboard alerts.</summary>
public sealed class IntegrationConnectedBroadcastHandler : IEventHandler<IntegrationConnectedEvent>
{
    private readonly IDashboardNotifier _notifier;
    public IntegrationConnectedBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(IntegrationConnectedEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId)) return Task.CompletedTask;
        return _notifier.NotifyChannelAsync(@event.BroadcasterId, "integration_connected", new
        {
            integration = @event.IntegrationName,
        }, ct);
    }
}

/// <summary>Broadcasts integration disconnection events as dashboard alerts.</summary>
public sealed class IntegrationDisconnectedBroadcastHandler : IEventHandler<IntegrationDisconnectedEvent>
{
    private readonly IDashboardNotifier _notifier;
    public IntegrationDisconnectedBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(IntegrationDisconnectedEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId)) return Task.CompletedTask;
        return _notifier.SendAlertAsync(@event.BroadcasterId, new AlertDto(
            Type: "integration_disconnected",
            Message: $"{@event.IntegrationName} disconnected",
            Data: new { integration = @event.IntegrationName }), ct);
    }
}
