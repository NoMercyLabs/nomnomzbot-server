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
/// Updates Channel.IsLive = false and cancels all running pipelines when
/// the stream goes offline via EventSub stream.offline.
/// Computes actual stream duration from ChannelContext.WentLiveAt.
/// </summary>
public sealed class ChannelOfflineHandler : IEventHandler<ChannelOfflineEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPipelineEngine _pipeline;
    private readonly IChannelRegistry _registry;
    private readonly ILogger<ChannelOfflineHandler> _logger;

    public ChannelOfflineHandler(
        IServiceScopeFactory scopeFactory,
        IPipelineEngine pipeline,
        IChannelRegistry registry,
        ILogger<ChannelOfflineHandler> logger
    )
    {
        _scopeFactory = scopeFactory;
        _pipeline = pipeline;
        _registry = registry;
        _logger = logger;
    }

    public async Task HandleAsync(
        ChannelOfflineEvent @event,
        CancellationToken cancellationToken = default
    )
    {
        var broadcasterId = @event.BroadcasterId;
        if (string.IsNullOrEmpty(broadcasterId))
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var channel = await db.Channels.FindAsync([broadcasterId], cancellationToken);
        if (channel is null)
        {
            _logger.LogWarning(
                "ChannelOfflineEvent received for unknown channel {BroadcasterId}",
                broadcasterId
            );
            return;
        }

        // Compute actual stream duration from ChannelContext before resetting state
        var channelCtx = _registry.Get(broadcasterId);
        var streamDuration =
            channelCtx?.WentLiveAt.HasValue == true
                ? DateTimeOffset.UtcNow - channelCtx.WentLiveAt.Value
                : @event.StreamDuration;

        if (channelCtx is not null)
        {
            channelCtx.IsLive = false;
            channelCtx.WentLiveAt = null;
        }

        channel.IsLive = false;
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Channel {BroadcasterId} went OFFLINE after {Duration}",
            broadcasterId,
            streamDuration
        );

        await _pipeline.CancelAllForChannelAsync(broadcasterId);

        await ExecuteEventResponseAsync(
            db,
            broadcasterId,
            "stream_offline",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["broadcaster"] = @event.BroadcasterDisplayName,
                ["duration"] = streamDuration.ToString(@"hh\:mm\:ss"),
            },
            cancellationToken
        );
    }

    private async Task ExecuteEventResponseAsync(
        IApplicationDbContext db,
        string broadcasterId,
        string eventType,
        Dictionary<string, string> variables,
        CancellationToken ct
    )
    {
        var config = await db.Records.FirstOrDefaultAsync(
            r => r.BroadcasterId == broadcasterId && r.RecordType == $"event_response:{eventType}",
            ct
        );

        if (config is null || string.IsNullOrWhiteSpace(config.Data))
            return;

        try
        {
            await _pipeline.ExecuteAsync(
                new PipelineRequest
                {
                    BroadcasterId = broadcasterId,
                    PipelineJson = config.Data,
                    TriggeredByUserId = broadcasterId,
                    TriggeredByDisplayName = string.Empty,
                    RawMessage = string.Empty,
                    InitialVariables = variables,
                },
                ct
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to execute event_response pipeline for {EventType} in {Channel}",
                eventType,
                broadcasterId
            );
        }
    }
}
