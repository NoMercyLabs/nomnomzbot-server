// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.EventHandlers;

/// <summary>
/// Updates Channel.IsLive = true, refreshes title/game, and creates a Stream record
/// when a stream comes online via EventSub stream.online.
/// </summary>
public sealed class ChannelOnlineHandler : IEventHandler<ChannelOnlineEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPipelineEngine _pipeline;
    private readonly ILogger<ChannelOnlineHandler> _logger;

    public ChannelOnlineHandler(
        IServiceScopeFactory scopeFactory,
        IPipelineEngine pipeline,
        ILogger<ChannelOnlineHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task HandleAsync(ChannelOnlineEvent @event, CancellationToken cancellationToken = default)
    {
        var broadcasterId = @event.BroadcasterId;
        if (string.IsNullOrEmpty(broadcasterId)) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var channel = await db.Channels.FindAsync([broadcasterId], cancellationToken);
        if (channel is null)
        {
            _logger.LogWarning("ChannelOnlineEvent received for unknown channel {BroadcasterId}", broadcasterId);
            return;
        }

        channel.IsLive = true;
        channel.Title = @event.StreamTitle;
        channel.GameName = @event.GameName;

        // Create stream record for this session
        var stream = new NoMercyBot.Domain.Entities.Stream
        {
            Id = Ulid.NewUlid().ToString(),
            ChannelId = broadcasterId,
            Title = @event.StreamTitle,
            GameName = @event.GameName,
        };
        db.Streams.Add(stream);

        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Channel {BroadcasterId} is now LIVE: {Title} playing {Game}",
            broadcasterId, @event.StreamTitle, @event.GameName);

        // Execute user-configured online pipeline if present
        await ExecuteEventResponseAsync(db, broadcasterId, "stream_online",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["broadcaster"] = @event.BroadcasterDisplayName,
                ["title"] = @event.StreamTitle,
                ["game"] = @event.GameName,
            }, cancellationToken);
    }

    private async Task ExecuteEventResponseAsync(
        IApplicationDbContext db,
        string broadcasterId,
        string eventType,
        Dictionary<string, string> variables,
        CancellationToken ct)
    {
        var config = await db.Records
            .FirstOrDefaultAsync(r =>
                r.BroadcasterId == broadcasterId
                && r.RecordType == $"event_response:{eventType}", ct);

        if (config is null || string.IsNullOrWhiteSpace(config.Data)) return;

        try
        {
            await _pipeline.ExecuteAsync(new PipelineRequest
            {
                BroadcasterId = broadcasterId,
                PipelineJson = config.Data,
                TriggeredByUserId = broadcasterId,
                TriggeredByDisplayName = string.Empty,
                RawMessage = string.Empty,
                InitialVariables = variables,
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute event_response pipeline for {EventType} in {Channel}",
                eventType, broadcasterId);
        }
    }
}

/// <summary>
/// Updates Channel.IsLive = false and cancels all running pipelines when
/// the stream goes offline via EventSub stream.offline.
/// </summary>
public sealed class ChannelOfflineHandler : IEventHandler<ChannelOfflineEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPipelineEngine _pipeline;
    private readonly ILogger<ChannelOfflineHandler> _logger;

    public ChannelOfflineHandler(
        IServiceScopeFactory scopeFactory,
        IPipelineEngine pipeline,
        ILogger<ChannelOfflineHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task HandleAsync(ChannelOfflineEvent @event, CancellationToken cancellationToken = default)
    {
        var broadcasterId = @event.BroadcasterId;
        if (string.IsNullOrEmpty(broadcasterId)) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var channel = await db.Channels.FindAsync([broadcasterId], cancellationToken);
        if (channel is null)
        {
            _logger.LogWarning("ChannelOfflineEvent received for unknown channel {BroadcasterId}", broadcasterId);
            return;
        }

        channel.IsLive = false;
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Channel {BroadcasterId} went OFFLINE after {Duration}",
            broadcasterId, @event.StreamDuration);

        // Cancel all running pipelines for this channel
        await _pipeline.CancelAllForChannelAsync(broadcasterId);

        // Execute user-configured offline pipeline if present
        await ExecuteEventResponseAsync(db, broadcasterId, "stream_offline",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["broadcaster"] = @event.BroadcasterDisplayName,
                ["duration"] = @event.StreamDuration.ToString(@"hh\:mm\:ss"),
            }, cancellationToken);
    }

    private async Task ExecuteEventResponseAsync(
        IApplicationDbContext db,
        string broadcasterId,
        string eventType,
        Dictionary<string, string> variables,
        CancellationToken ct)
    {
        var config = await db.Records
            .FirstOrDefaultAsync(r =>
                r.BroadcasterId == broadcasterId
                && r.RecordType == $"event_response:{eventType}", ct);

        if (config is null || string.IsNullOrWhiteSpace(config.Data)) return;

        try
        {
            await _pipeline.ExecuteAsync(new PipelineRequest
            {
                BroadcasterId = broadcasterId,
                PipelineJson = config.Data,
                TriggeredByUserId = broadcasterId,
                TriggeredByDisplayName = string.Empty,
                RawMessage = string.Empty,
                InitialVariables = variables,
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute event_response pipeline for {EventType} in {Channel}",
                eventType, broadcasterId);
        }
    }
}

/// <summary>
/// Updates Channel.Title and Channel.GameName when the channel info changes.
/// </summary>
public sealed class ChannelUpdatedHandler : IEventHandler<ChannelUpdatedEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChannelUpdatedHandler> _logger;

    public ChannelUpdatedHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<ChannelUpdatedHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(ChannelUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        var broadcasterId = @event.BroadcasterId;
        if (string.IsNullOrEmpty(broadcasterId)) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var channel = await db.Channels.FindAsync([broadcasterId], cancellationToken);
        if (channel is null) return;

        channel.Title = @event.NewTitle;
        channel.GameName = @event.NewGameName;
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Channel {BroadcasterId} updated: title={Title}, game={Game}",
            broadcasterId, @event.NewTitle, @event.NewGameName);
    }
}
