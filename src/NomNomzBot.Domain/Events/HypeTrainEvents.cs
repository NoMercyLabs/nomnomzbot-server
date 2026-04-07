// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

/// <summary>Published when a hype train begins.</summary>
public sealed class HypeTrainBeganEvent : DomainEventBase
{
    public required string HypeTrainId { get; init; }
    public required int Level { get; init; }
    public required int Total { get; init; }
    public required int Goal { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}

/// <summary>Published when a hype train ends.</summary>
public sealed class HypeTrainEndedEvent : DomainEventBase
{
    public required string HypeTrainId { get; init; }
    public required int Level { get; init; }
    public required int Total { get; init; }
}
