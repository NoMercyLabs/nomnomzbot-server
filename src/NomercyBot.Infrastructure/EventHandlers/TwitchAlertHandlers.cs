// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Domain.Entities;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.EventHandlers;

/// <summary>
/// Base class for stream engagement event handlers.
/// Logs the event to ChannelEvents and executes the user-configured pipeline
/// stored in Records with RecordType = "event_response:{eventType}".
/// If no config exists, does nothing (no hardcoded behavior).
/// </summary>
public abstract class TwitchAlertHandlerBase<TEvent>
    where TEvent : class, IDomainEvent
{
    protected abstract string EventTypeKey { get; }

    protected readonly IServiceScopeFactory ScopeFactory;
    protected readonly IPipelineEngine Pipeline;
    protected readonly ILogger Logger;

    protected TwitchAlertHandlerBase(
        IServiceScopeFactory scopeFactory,
        IPipelineEngine pipeline,
        ILogger logger
    )
    {
        ScopeFactory = scopeFactory;
        Pipeline = pipeline;
        Logger = logger;
    }

    protected abstract string? GetUserId(TEvent @event);
    protected abstract string? GetUserDisplayName(TEvent @event);
    protected abstract Dictionary<string, string> BuildVariables(TEvent @event);

    protected async Task HandleCoreAsync(TEvent @event, CancellationToken ct)
    {
        var broadcasterId = @event.BroadcasterId;
        if (string.IsNullOrEmpty(broadcasterId))
            return;

        using var scope = ScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Persist to ChannelEvents for analytics
        await LogChannelEventAsync(db, @event, broadcasterId, ct);

        // Look up user-configured pipeline response
        var config = await db.Records.FirstOrDefaultAsync(
            r =>
                r.BroadcasterId == broadcasterId
                && r.RecordType == $"event_response:{EventTypeKey}",
            ct
        );

        if (config is null || string.IsNullOrWhiteSpace(config.Data))
            return;

        var variables = BuildVariables(@event);

        Logger.LogDebug(
            "Executing event_response:{EventType} pipeline for channel {Channel}",
            EventTypeKey,
            broadcasterId
        );

        try
        {
            await Pipeline.ExecuteAsync(
                new PipelineRequest
                {
                    BroadcasterId = broadcasterId,
                    PipelineJson = config.Data,
                    TriggeredByUserId = GetUserId(@event) ?? broadcasterId,
                    TriggeredByDisplayName = GetUserDisplayName(@event) ?? string.Empty,
                    RawMessage = string.Empty,
                    InitialVariables = variables,
                },
                ct
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to execute event_response:{EventType} pipeline in {Channel}",
                EventTypeKey,
                broadcasterId
            );
        }
    }

    private async Task LogChannelEventAsync(
        IApplicationDbContext db,
        TEvent @event,
        string broadcasterId,
        CancellationToken ct
    )
    {
        try
        {
            var variables = BuildVariables(@event);
            db.ChannelEvents.Add(
                new ChannelEvent
                {
                    Id = Ulid.NewUlid().ToString(),
                    ChannelId = broadcasterId,
                    UserId = GetUserId(@event),
                    Type = EventTypeKey,
                    Data = JsonSerializer.Serialize(variables),
                }
            );
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to log ChannelEvent {EventType} for {Channel}",
                EventTypeKey,
                broadcasterId
            );
        }
    }
}

// ─── Follow ──────────────────────────────────────────────────────────────────

public sealed class FollowEventHandler
    : TwitchAlertHandlerBase<FollowEvent>,
        IEventHandler<FollowEvent>
{
    protected override string EventTypeKey => "follow";

    public FollowEventHandler(
        IServiceScopeFactory s,
        IPipelineEngine p,
        ILogger<FollowEventHandler> l
    )
        : base(s, p, l) { }

    protected override string? GetUserId(FollowEvent e) => e.UserId;

    protected override string? GetUserDisplayName(FollowEvent e) => e.UserDisplayName;

    protected override Dictionary<string, string> BuildVariables(FollowEvent e) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["user"] = e.UserDisplayName,
            ["user.id"] = e.UserId,
            ["user.name"] = e.UserLogin,
            ["followed_at"] = e.FollowedAt.ToString("O"),
        };

    public Task HandleAsync(FollowEvent @event, CancellationToken ct = default) =>
        HandleCoreAsync(@event, ct);
}

// ─── NewFollower (duplicate of Follow, from IRC path) ────────────────────────

public sealed class NewFollowerEventHandler
    : TwitchAlertHandlerBase<NewFollowerEvent>,
        IEventHandler<NewFollowerEvent>
{
    protected override string EventTypeKey => "follow";

    public NewFollowerEventHandler(
        IServiceScopeFactory s,
        IPipelineEngine p,
        ILogger<NewFollowerEventHandler> l
    )
        : base(s, p, l) { }

    protected override string? GetUserId(NewFollowerEvent e) => e.UserId;

    protected override string? GetUserDisplayName(NewFollowerEvent e) => e.UserDisplayName;

    protected override Dictionary<string, string> BuildVariables(NewFollowerEvent e) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["user"] = e.UserDisplayName,
            ["user.id"] = e.UserId,
            ["user.name"] = e.UserLogin,
        };

    public Task HandleAsync(NewFollowerEvent @event, CancellationToken ct = default) =>
        HandleCoreAsync(@event, ct);
}

// ─── Subscription ─────────────────────────────────────────────────────────────

public sealed class NewSubscriptionEventHandler
    : TwitchAlertHandlerBase<NewSubscriptionEvent>,
        IEventHandler<NewSubscriptionEvent>
{
    protected override string EventTypeKey => "subscription";

    public NewSubscriptionEventHandler(
        IServiceScopeFactory s,
        IPipelineEngine p,
        ILogger<NewSubscriptionEventHandler> l
    )
        : base(s, p, l) { }

    protected override string? GetUserId(NewSubscriptionEvent e) => e.UserId;

    protected override string? GetUserDisplayName(NewSubscriptionEvent e) => e.UserDisplayName;

    protected override Dictionary<string, string> BuildVariables(NewSubscriptionEvent e) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["user"] = e.UserDisplayName,
            ["user.id"] = e.UserId,
            ["tier"] = e.Tier,
        };

    public Task HandleAsync(NewSubscriptionEvent @event, CancellationToken ct = default) =>
        HandleCoreAsync(@event, ct);
}

public sealed class ResubscriptionEventHandler
    : TwitchAlertHandlerBase<ResubscriptionEvent>,
        IEventHandler<ResubscriptionEvent>
{
    protected override string EventTypeKey => "resub";

    public ResubscriptionEventHandler(
        IServiceScopeFactory s,
        IPipelineEngine p,
        ILogger<ResubscriptionEventHandler> l
    )
        : base(s, p, l) { }

    protected override string? GetUserId(ResubscriptionEvent e) => e.UserId;

    protected override string? GetUserDisplayName(ResubscriptionEvent e) => e.UserDisplayName;

    protected override Dictionary<string, string> BuildVariables(ResubscriptionEvent e) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["user"] = e.UserDisplayName,
            ["user.id"] = e.UserId,
            ["tier"] = e.Tier,
            ["months"] = e.CumulativeMonths.ToString(),
            ["streak"] = e.StreakMonths.ToString(),
            ["message"] = e.Message ?? string.Empty,
        };

    public Task HandleAsync(ResubscriptionEvent @event, CancellationToken ct = default) =>
        HandleCoreAsync(@event, ct);
}

// ─── Gift Subscription ────────────────────────────────────────────────────────

public sealed class GiftSubscriptionEventHandler
    : TwitchAlertHandlerBase<GiftSubscriptionEvent>,
        IEventHandler<GiftSubscriptionEvent>
{
    protected override string EventTypeKey => "gift_sub";

    public GiftSubscriptionEventHandler(
        IServiceScopeFactory s,
        IPipelineEngine p,
        ILogger<GiftSubscriptionEventHandler> l
    )
        : base(s, p, l) { }

    protected override string? GetUserId(GiftSubscriptionEvent e) =>
        e.IsAnonymous ? null : e.GifterUserId;

    protected override string? GetUserDisplayName(GiftSubscriptionEvent e) =>
        e.IsAnonymous ? "Anonymous" : e.GifterDisplayName;

    protected override Dictionary<string, string> BuildVariables(GiftSubscriptionEvent e) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["user"] = e.IsAnonymous ? "Anonymous" : e.GifterDisplayName,
            ["user.id"] = e.IsAnonymous ? string.Empty : e.GifterUserId,
            ["tier"] = e.Tier,
            ["count"] = e.GiftCount.ToString(),
            ["anonymous"] = e.IsAnonymous ? "true" : "false",
        };

    public Task HandleAsync(GiftSubscriptionEvent @event, CancellationToken ct = default) =>
        HandleCoreAsync(@event, ct);
}

// ─── Cheer ────────────────────────────────────────────────────────────────────

public sealed class CheerEventHandler
    : TwitchAlertHandlerBase<CheerEvent>,
        IEventHandler<CheerEvent>
{
    protected override string EventTypeKey => "cheer";

    public CheerEventHandler(
        IServiceScopeFactory s,
        IPipelineEngine p,
        ILogger<CheerEventHandler> l
    )
        : base(s, p, l) { }

    protected override string? GetUserId(CheerEvent e) => e.IsAnonymous ? null : e.UserId;

    protected override string? GetUserDisplayName(CheerEvent e) =>
        e.IsAnonymous ? "Anonymous" : e.UserDisplayName;

    protected override Dictionary<string, string> BuildVariables(CheerEvent e) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["user"] = e.IsAnonymous ? "Anonymous" : e.UserDisplayName,
            ["user.id"] = e.IsAnonymous ? string.Empty : e.UserId,
            ["bits"] = e.Bits.ToString(),
            ["message"] = e.Message,
            ["anonymous"] = e.IsAnonymous ? "true" : "false",
        };

    public Task HandleAsync(CheerEvent @event, CancellationToken ct = default) =>
        HandleCoreAsync(@event, ct);
}

// ─── Raid ─────────────────────────────────────────────────────────────────────

public sealed class RaidEventHandler : TwitchAlertHandlerBase<RaidEvent>, IEventHandler<RaidEvent>
{
    protected override string EventTypeKey => "raid";

    public RaidEventHandler(IServiceScopeFactory s, IPipelineEngine p, ILogger<RaidEventHandler> l)
        : base(s, p, l) { }

    protected override string? GetUserId(RaidEvent e) => e.FromUserId;

    protected override string? GetUserDisplayName(RaidEvent e) => e.FromDisplayName;

    protected override Dictionary<string, string> BuildVariables(RaidEvent e) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["user"] = e.FromDisplayName,
            ["user.id"] = e.FromUserId,
            ["user.name"] = e.FromLogin,
            ["viewers"] = e.ViewerCount.ToString(),
        };

    public Task HandleAsync(RaidEvent @event, CancellationToken ct = default) =>
        HandleCoreAsync(@event, ct);
}
