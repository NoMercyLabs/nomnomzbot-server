// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

/// <summary>
/// Published for new subscriptions and resubscriptions.
/// </summary>
public sealed class SubscriptionEvent : DomainEventBase
{
    public required string UserId { get; init; }
    public required string UserDisplayName { get; init; }
    public required string Tier { get; init; }
    public required bool IsGift { get; init; }
    public int CumulativeMonths { get; init; }
    public int StreakMonths { get; init; }
    public string? Message { get; init; }
}
