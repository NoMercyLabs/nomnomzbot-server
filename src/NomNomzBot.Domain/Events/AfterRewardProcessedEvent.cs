// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class AfterRewardProcessedEvent : DomainEventBase
{
    public required string RewardId { get; init; }
    public required string RedemptionId { get; init; }
    public required bool Succeeded { get; init; }
    public required TimeSpan Duration { get; init; }
}
