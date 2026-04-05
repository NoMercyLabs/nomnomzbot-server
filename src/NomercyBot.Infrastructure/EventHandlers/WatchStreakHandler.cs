// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Domain.Entities;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.EventHandlers;

/// <summary>
/// Handles IRC viewermilestone events (watch streaks).
/// Upserts the WatchStreak entity and executes the event_response:watch_streak pipeline.
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

    public async Task HandleAsync(WatchStreakReceivedEvent @event, CancellationToken ct = default)
    {
        await UpsertStreakAsync(@event, ct);
        await HandleCoreAsync(@event, ct);
    }

    private async Task UpsertStreakAsync(WatchStreakReceivedEvent e, CancellationToken ct)
    {
        try
        {
            using IServiceScope scope = ScopeFactory.CreateScope();
            IApplicationDbContext db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            string? broadcasterId = e.BroadcasterId;
            if (string.IsNullOrEmpty(broadcasterId))
                return;

            WatchStreak? existing = await db.WatchStreaks.FirstOrDefaultAsync(
                w => w.BroadcasterId == broadcasterId && w.UserId == e.UserId,
                ct
            );

            if (existing is null)
            {
                db.WatchStreaks.Add(
                    new()
                    {
                        Id = Guid.NewGuid(),
                        BroadcasterId = broadcasterId,
                        UserId = e.UserId,
                        UserDisplayName = e.UserDisplayName,
                        CurrentStreak = e.StreakMonths,
                        MaxStreak = e.StreakMonths,
                        LastSeenDate = today,
                    }
                );
            }
            else
            {
                existing.UserDisplayName = e.UserDisplayName;
                existing.CurrentStreak = e.StreakMonths;
                if (e.StreakMonths > existing.MaxStreak)
                    existing.MaxStreak = e.StreakMonths;
                existing.LastSeenDate = today;
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to upsert WatchStreak for user {UserId} in channel {BroadcasterId}",
                e.UserId,
                e.BroadcasterId
            );
        }
    }
}
