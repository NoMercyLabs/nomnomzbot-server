// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

/// <summary>Published when an external integration disconnects.</summary>
public sealed class IntegrationDisconnectedEvent : DomainEventBase
{
    public required string IntegrationName { get; init; }
}
