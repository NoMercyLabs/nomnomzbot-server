// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

/// <summary>Published when an external integration encounters an error.</summary>
public sealed class IntegrationErrorEvent : DomainEventBase
{
    public required string IntegrationName { get; init; }
    public required string ErrorMessage { get; init; }
}
