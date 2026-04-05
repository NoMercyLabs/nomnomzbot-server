// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

/// <summary>Published when a prediction opens for voting.</summary>
public sealed class PredictionBeganEvent : DomainEventBase
{
    public required string PredictionId { get; init; }
    public required string Title { get; init; }
    public required IReadOnlyList<PredictionOutcome> Outcomes { get; init; }
    public required int WindowSeconds { get; init; }
    public required DateTimeOffset LocksAt { get; init; }
}

/// <summary>Published when voting is locked (but not yet resolved).</summary>
public sealed class PredictionLockedEvent : DomainEventBase
{
    public required string PredictionId { get; init; }
    public required string Title { get; init; }
    public required IReadOnlyList<PredictionOutcome> Outcomes { get; init; }
}

/// <summary>Published when a prediction is resolved or cancelled.</summary>
public sealed class PredictionEndedEvent : DomainEventBase
{
    public required string PredictionId { get; init; }
    public required string Title { get; init; }
    public required string Status { get; init; }
    public required IReadOnlyList<PredictionOutcome> Outcomes { get; init; }
    public string? WinningOutcomeId { get; init; }
}

public sealed record PredictionOutcome(
    string Id,
    string Title,
    int ChannelPoints,
    int Users,
    string Color
);
