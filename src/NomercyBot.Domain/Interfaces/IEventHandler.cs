// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using NoMercyBot.Domain.Events;

namespace NoMercyBot.Domain.Interfaces;

/// <summary>
/// Handler for a specific domain event type. Implementations must be safe
/// to call concurrently. Exceptions thrown here are caught by the EventBus
/// and logged -- they do NOT propagate to the publisher or other handlers.
/// </summary>
public interface IEventHandler<in TEvent> where TEvent : class, IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
