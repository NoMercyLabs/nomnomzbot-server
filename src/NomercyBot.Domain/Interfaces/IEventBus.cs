// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Domain.Events;

namespace NoMercyBot.Domain.Interfaces;

/// <summary>
/// The single interface for publishing domain events. Registered as a singleton in DI.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to all registered handlers.
    /// Handlers execute asynchronously. One handler's failure does not
    /// affect other handlers.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IDomainEvent;

    /// <summary>
    /// Publishes an event without awaiting handler completion.
    /// All handlers execute in the background. Failures are logged but not propagated.
    /// </summary>
    void PublishFireAndForget<TEvent>(TEvent @event)
        where TEvent : class, IDomainEvent;
}
