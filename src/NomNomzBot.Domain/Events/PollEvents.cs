// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

/// <summary>Published when a poll begins.</summary>
public sealed class PollBeganEvent : DomainEventBase
{
    public required string PollId { get; init; }
    public required string Title { get; init; }
    public required IReadOnlyList<PollChoice> Choices { get; init; }
    public required int DurationSeconds { get; init; }
    public required DateTimeOffset EndsAt { get; init; }
}

/// <summary>Published when a poll ends (terminal states: completed, archived, terminated).</summary>
public sealed class PollEndedEvent : DomainEventBase
{
    public required string PollId { get; init; }
    public required string Title { get; init; }
    public required string Status { get; init; }
    public required IReadOnlyList<PollChoice> Choices { get; init; }
}

public sealed record PollChoice(string Id, string Title, int Votes, int ChannelPointsVotes);
