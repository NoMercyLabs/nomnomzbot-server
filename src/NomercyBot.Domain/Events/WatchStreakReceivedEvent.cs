// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

/// <summary>
/// Published when a viewer reaches a watch streak milestone.
/// Sourced from IRC USERNOTICE with msg-id=viewermilestone.
/// </summary>
public sealed class WatchStreakReceivedEvent : DomainEventBase
{
    public required string UserId { get; init; }
    public required string UserLogin { get; init; }
    public required string UserDisplayName { get; init; }
    public required int StreakMonths { get; init; }
    public required int ChannelPointsEarned { get; init; }
    public string? CustomMessage { get; init; }
}
