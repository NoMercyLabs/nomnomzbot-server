// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.EventHandlers;

/// <summary>
/// Handles IRC viewermilestone events (watch streaks).
/// Executes the event_response:watch_streak pipeline if configured.
/// Variables exposed: user.id, user.name, streak.months, streak.points
/// </summary>
public sealed class WatchStreakHandler
    : TwitchAlertHandlerBase<WatchStreakReceivedEvent>,
        IEventHandler<WatchStreakReceivedEvent>
{
    protected override string EventTypeKey => "watch_streak";

    public WatchStreakHandler(
        IServiceScopeFactory s,
        IPipelineEngine p,
        ILogger<WatchStreakHandler> l
    )
        : base(s, p, l) { }

    protected override string? GetUserId(WatchStreakReceivedEvent e) => e.UserId;

    protected override string? GetUserDisplayName(WatchStreakReceivedEvent e) => e.UserDisplayName;

    protected override Dictionary<string, string> BuildVariables(WatchStreakReceivedEvent e) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["user.id"] = e.UserId,
            ["user.login"] = e.UserLogin,
            ["user.name"] = e.UserDisplayName,
            ["streak.months"] = e.StreakMonths.ToString(),
            ["streak.points"] = e.ChannelPointsEarned.ToString(),
            ["streak.message"] = e.CustomMessage ?? string.Empty,
        };

    public Task HandleAsync(WatchStreakReceivedEvent @event, CancellationToken ct = default) =>
        HandleCoreAsync(@event, ct);
}
