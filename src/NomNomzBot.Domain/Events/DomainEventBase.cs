// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

/// <summary>
/// Abstract base class implementing IDomainEvent with sensible defaults.
/// All concrete domain events should inherit from this class.
/// </summary>
public abstract class DomainEventBase : IDomainEvent
{
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string? BroadcasterId { get; init; }
}
