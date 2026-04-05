// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Api.Hubs.Dtos;
using NoMercyBot.Domain.Events;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Api.Hubs.EventHandlers;

/// <summary>Broadcasts follow alerts to dashboard/overlay clients.</summary>
public sealed class FollowBroadcastHandler : IEventHandler<FollowEvent>
{
    private readonly IDashboardNotifier _notifier;

    public FollowBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(FollowEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.NotifyChannelAsync(
            @event.BroadcasterId,
            "follow",
            new FollowAlertDto(
                @event.UserId,
                @event.UserDisplayName,
                @event.UserLogin,
                @event.FollowedAt
            ),
            ct
        );
    }
}

/// <summary>Broadcasts new follower alerts (IRC fallback path) to dashboard/overlay clients.</summary>
public sealed class NewFollowerBroadcastHandler : IEventHandler<NewFollowerEvent>
{
    private readonly IDashboardNotifier _notifier;

    public NewFollowerBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(NewFollowerEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.NotifyChannelAsync(
            @event.BroadcasterId,
            "follow",
            new FollowAlertDto(@event.UserId, @event.UserDisplayName, @event.UserLogin, null),
            ct
        );
    }
}

/// <summary>Broadcasts new subscription alerts to dashboard/overlay clients.</summary>
public sealed class NewSubscriptionBroadcastHandler : IEventHandler<NewSubscriptionEvent>
{
    private readonly IDashboardNotifier _notifier;

    public NewSubscriptionBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(NewSubscriptionEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.NotifyChannelAsync(
            @event.BroadcasterId,
            "subscription",
            new SubscriptionAlertDto(@event.UserId, @event.UserDisplayName, @event.Tier),
            ct
        );
    }
}

/// <summary>Broadcasts resubscription alerts to dashboard/overlay clients.</summary>
public sealed class ResubscriptionBroadcastHandler : IEventHandler<ResubscriptionEvent>
{
    private readonly IDashboardNotifier _notifier;

    public ResubscriptionBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(ResubscriptionEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.NotifyChannelAsync(
            @event.BroadcasterId,
            "resub",
            new ResubAlertDto(
                @event.UserId,
                @event.UserDisplayName,
                @event.Tier,
                @event.CumulativeMonths,
                @event.StreakMonths,
                @event.Message
            ),
            ct
        );
    }
}

/// <summary>Broadcasts gift subscription alerts to dashboard/overlay clients.</summary>
public sealed class GiftSubscriptionBroadcastHandler : IEventHandler<GiftSubscriptionEvent>
{
    private readonly IDashboardNotifier _notifier;

    public GiftSubscriptionBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(GiftSubscriptionEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.NotifyChannelAsync(
            @event.BroadcasterId,
            "gift_sub",
            new GiftSubAlertDto(
                @event.IsAnonymous ? null : @event.GifterUserId,
                @event.IsAnonymous ? "Anonymous" : @event.GifterDisplayName,
                @event.Tier,
                @event.GiftCount,
                @event.IsAnonymous
            ),
            ct
        );
    }
}

/// <summary>Broadcasts cheer/bits alerts to dashboard/overlay clients.</summary>
public sealed class CheerBroadcastHandler : IEventHandler<CheerEvent>
{
    private readonly IDashboardNotifier _notifier;

    public CheerBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(CheerEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.NotifyChannelAsync(
            @event.BroadcasterId,
            "cheer",
            new CheerAlertDto(
                @event.IsAnonymous ? null : @event.UserId,
                @event.IsAnonymous ? "Anonymous" : @event.UserDisplayName,
                @event.Bits,
                @event.Message,
                @event.IsAnonymous
            ),
            ct
        );
    }
}

/// <summary>Broadcasts incoming raid alerts to dashboard/overlay clients.</summary>
public sealed class RaidBroadcastHandler : IEventHandler<RaidEvent>
{
    private readonly IDashboardNotifier _notifier;

    public RaidBroadcastHandler(IDashboardNotifier notifier) => _notifier = notifier;

    public Task HandleAsync(RaidEvent @event, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(@event.BroadcasterId))
            return Task.CompletedTask;

        return _notifier.NotifyChannelAsync(
            @event.BroadcasterId,
            "raid",
            new RaidAlertDto(
                @event.FromUserId,
                @event.FromDisplayName,
                @event.FromLogin,
                @event.ViewerCount
            ),
            ct
        );
    }
}
