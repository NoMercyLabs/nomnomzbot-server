// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.EventHandlers;

/// <summary>
/// Updates Channel.Title and Channel.GameName when the channel info changes.
/// </summary>
public sealed class ChannelUpdatedHandler : IEventHandler<ChannelUpdatedEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChannelUpdatedHandler> _logger;

    public ChannelUpdatedHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<ChannelUpdatedHandler> logger
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(
        ChannelUpdatedEvent @event,
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
            return;

        channel.Title = @event.NewTitle;
        channel.GameName = @event.NewGameName;
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Channel {BroadcasterId} updated: title={Title}, game={Game}",
            broadcasterId,
            @event.NewTitle,
            @event.NewGameName
        );
    }
}
