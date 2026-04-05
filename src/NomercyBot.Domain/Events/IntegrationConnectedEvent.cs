// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

/// <summary>Published when an external integration (e.g. "spotify", "discord", "obs") connects successfully.</summary>
public sealed class IntegrationConnectedEvent : DomainEventBase
{
    public required string IntegrationName { get; init; }
}
