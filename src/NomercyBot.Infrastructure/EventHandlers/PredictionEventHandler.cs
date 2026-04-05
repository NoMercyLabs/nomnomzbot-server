// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.EventHandlers;

/// <summary>Handles prediction begin events.</summary>
public sealed class PredictionBeganHandler
    : TwitchAlertHandlerBase<PredictionBeganEvent>,
        IEventHandler<PredictionBeganEvent>
{
    protected override string EventTypeKey => "prediction_begin";

    public PredictionBeganHandler(
        IServiceScopeFactory s,
        IPipelineEngine p,
        ILogger<PredictionBeganHandler> l
    )
        : base(s, p, l) { }

    protected override string? GetUserId(PredictionBeganEvent e) => e.BroadcasterId;

    protected override string? GetUserDisplayName(PredictionBeganEvent e) => null;

    protected override Dictionary<string, string> BuildVariables(PredictionBeganEvent e) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["prediction.id"] = e.PredictionId,
            ["prediction.title"] = e.Title,
            ["prediction.window"] = e.WindowSeconds.ToString(),
            ["prediction.outcomes"] = string.Join(", ", e.Outcomes.Select(o => o.Title)),
        };

    public Task HandleAsync(PredictionBeganEvent @event, CancellationToken ct = default) =>
        HandleCoreAsync(@event, ct);
}

/// <summary>Handles prediction locked events.</summary>
public sealed class PredictionLockedHandler
    : TwitchAlertHandlerBase<PredictionLockedEvent>,
        IEventHandler<PredictionLockedEvent>
{
    protected override string EventTypeKey => "prediction_lock";

    public PredictionLockedHandler(
        IServiceScopeFactory s,
        IPipelineEngine p,
        ILogger<PredictionLockedHandler> l
    )
        : base(s, p, l) { }

    protected override string? GetUserId(PredictionLockedEvent e) => e.BroadcasterId;

    protected override string? GetUserDisplayName(PredictionLockedEvent e) => null;

    protected override Dictionary<string, string> BuildVariables(PredictionLockedEvent e) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["prediction.id"] = e.PredictionId,
            ["prediction.title"] = e.Title,
            ["prediction.total_points"] = e.Outcomes.Sum(o => o.ChannelPoints).ToString(),
        };

    public Task HandleAsync(PredictionLockedEvent @event, CancellationToken ct = default) =>
        HandleCoreAsync(@event, ct);
}

/// <summary>Handles prediction ended events.</summary>
public sealed class PredictionEndedHandler
    : TwitchAlertHandlerBase<PredictionEndedEvent>,
        IEventHandler<PredictionEndedEvent>
{
    protected override string EventTypeKey => "prediction_end";

    public PredictionEndedHandler(
        IServiceScopeFactory s,
        IPipelineEngine p,
        ILogger<PredictionEndedHandler> l
    )
        : base(s, p, l) { }

    protected override string? GetUserId(PredictionEndedEvent e) => e.BroadcasterId;

    protected override string? GetUserDisplayName(PredictionEndedEvent e) => null;

    protected override Dictionary<string, string> BuildVariables(PredictionEndedEvent e)
    {
        var winner = e.Outcomes.FirstOrDefault(o => o.Id == e.WinningOutcomeId);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["prediction.id"] = e.PredictionId,
            ["prediction.title"] = e.Title,
            ["prediction.status"] = e.Status,
            ["prediction.winner"] = winner?.Title ?? string.Empty,
            ["prediction.winner.points"] = winner?.ChannelPoints.ToString() ?? "0",
            ["prediction.winner.users"] = winner?.Users.ToString() ?? "0",
        };
    }

    public Task HandleAsync(PredictionEndedEvent @event, CancellationToken ct = default) =>
        HandleCoreAsync(@event, ct);
}
