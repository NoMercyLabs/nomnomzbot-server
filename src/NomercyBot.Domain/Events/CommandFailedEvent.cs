// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class CommandFailedEvent : DomainEventBase
{
    public required string CommandName { get; init; }
    public required string TriggeredByUserId { get; init; }
    public required string Reason { get; init; }
}
