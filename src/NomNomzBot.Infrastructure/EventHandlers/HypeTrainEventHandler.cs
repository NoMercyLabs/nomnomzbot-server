// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.EventHandlers;

/// <summary>Handles hype train begin events.</summary>
public sealed class HypeTrainBeganHandler
    : TwitchAlertHandlerBase<HypeTrainBeganEvent>,
        IEventHandler<HypeTrainBeganEvent>
{
    protected override string EventTypeKey => "hype_train_begin";

    public HypeTrainBeganHandler(
        IServiceScopeFactory s,
        IPipelineEngine p,
        ILogger<HypeTrainBeganHandler> l
    )
        : base(s, p, l) { }

    protected override string? GetUserId(HypeTrainBeganEvent e) => e.BroadcasterId;

    protected override string? GetUserDisplayName(HypeTrainBeganEvent e) => null;

    protected override Dictionary<string, string> BuildVariables(HypeTrainBeganEvent e) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["hype_train.id"] = e.HypeTrainId,
            ["hype_train.level"] = e.Level.ToString(),
            ["hype_train.total"] = e.Total.ToString(),
            ["hype_train.goal"] = e.Goal.ToString(),
        };

    public Task HandleAsync(HypeTrainBeganEvent @event, CancellationToken ct = default) =>
        HandleCoreAsync(@event, ct);
}

/// <summary>Handles hype train ended events.</summary>
public sealed class HypeTrainEndedHandler
    : TwitchAlertHandlerBase<HypeTrainEndedEvent>,
        IEventHandler<HypeTrainEndedEvent>
{
    protected override string EventTypeKey => "hype_train_end";

    public HypeTrainEndedHandler(
        IServiceScopeFactory s,
        IPipelineEngine p,
        ILogger<HypeTrainEndedHandler> l
    )
        : base(s, p, l) { }

    protected override string? GetUserId(HypeTrainEndedEvent e) => e.BroadcasterId;

    protected override string? GetUserDisplayName(HypeTrainEndedEvent e) => null;

    protected override Dictionary<string, string> BuildVariables(HypeTrainEndedEvent e) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["hype_train.id"] = e.HypeTrainId,
            ["hype_train.level"] = e.Level.ToString(),
            ["hype_train.total"] = e.Total.ToString(),
        };

    public Task HandleAsync(HypeTrainEndedEvent @event, CancellationToken ct = default) =>
        HandleCoreAsync(@event, ct);
}
