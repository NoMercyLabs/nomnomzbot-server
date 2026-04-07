// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

/// <summary>
/// Published when a channel point reward is redeemed by a viewer.
/// </summary>
public sealed class RewardRedeemedEvent : DomainEventBase
{
    public required string RewardId { get; init; }
    public required string RewardTitle { get; init; }
    public required string RedemptionId { get; init; }
    public required string UserId { get; init; }
    public required string UserDisplayName { get; init; }
    public required int Cost { get; init; }
    public string? UserInput { get; init; }
}
