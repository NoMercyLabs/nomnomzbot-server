// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

/// <summary>
/// Marker interface for all domain events. Provides common metadata for tracing and routing.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Unique ID for this event instance. Used for deduplication and tracing.</summary>
    string EventId { get; }

    /// <summary>When this event occurred (UTC).</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>The broadcaster channel this event relates to, or null for platform-level events.</summary>
    string? BroadcasterId { get; }
}
