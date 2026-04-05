// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Domain.Events;

public sealed class AfterCommandExecutedEvent : DomainEventBase
{
    public required string CommandName { get; init; }
    public required string TriggeredByUserId { get; init; }
    public required TimeSpan Duration { get; init; }
    public required bool Succeeded { get; init; }
}
