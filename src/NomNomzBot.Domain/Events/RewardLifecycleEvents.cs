// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

/// <summary>Published when a custom channel point reward is created on Twitch.</summary>
public sealed class RewardCreatedEvent : DomainEventBase
{
    public required string TwitchRewardId { get; init; }
    public required string Title { get; init; }
    public required int Cost { get; init; }
    public required bool IsEnabled { get; init; }
}

/// <summary>Published when a custom channel point reward is updated on Twitch.</summary>
public sealed class RewardUpdatedEvent : DomainEventBase
{
    public required string TwitchRewardId { get; init; }
    public required string Title { get; init; }
    public required int Cost { get; init; }
    public required bool IsEnabled { get; init; }
}

/// <summary>Published when a custom channel point reward is removed from Twitch.</summary>
public sealed class RewardRemovedEvent : DomainEventBase
{
    public required string TwitchRewardId { get; init; }
    public required string Title { get; init; }
}
